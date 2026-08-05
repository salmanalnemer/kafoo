using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Kafo.Web.ViewModels.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminProfileController : Controller
{
    private const string EmailChangePortalType = "AdminEmailChange";

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILoginOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AdminProfileController> _logger;

    public AdminProfileController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILoginOtpService otpService,
        IEmailSender emailSender,
        ILogger<AdminProfileController> logger)
    {
        _context = context;
        _files = files;
        _otpService = otpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet("/Admin/Profile")]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentAdminUserAsync();

        if (user == null)
            return Redirect("/Admin/Login");

        var profile = await BuildProfileModelAsync(user);
        ViewBag.PasswordModel = new AdminChangePasswordViewModel();

        return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
    }

    [HttpPost("/Admin/Profile/UpdateInfo")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> UpdateInfo(
        [Bind(nameof(AdminProfileViewModel.FullName),
              nameof(AdminProfileViewModel.UserName),
              nameof(AdminProfileViewModel.Phone))]
        AdminProfileViewModel model,
        IFormFile? profileImage,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        // البريد لا يُحدّث من هذا المسار. له مسار مستقل يتطلب كلمة المرور وOTP.
        ModelState.Remove(nameof(model.Email));

        var normalizedFullName = (model.FullName ?? string.Empty).Trim();
        var normalizedUserName = (model.UserName ?? string.Empty).Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(model.Phone)
            ? null
            : model.Phone.Trim();

        if (string.IsNullOrWhiteSpace(normalizedFullName))
            ModelState.AddModelError(nameof(model.FullName), "الاسم مطلوب.");

        if (string.IsNullOrWhiteSpace(normalizedUserName))
            ModelState.AddModelError(nameof(model.UserName), "اسم المستخدم مطلوب.");

        if (normalizedFullName.Length > 180)
            ModelState.AddModelError(nameof(model.FullName), "الاسم يتجاوز الحد المسموح.");

        if (normalizedUserName.Length > 80)
            ModelState.AddModelError(nameof(model.UserName), "اسم المستخدم يتجاوز الحد المسموح.");

        if (normalizedPhone is { Length: > 40 })
            ModelState.AddModelError(nameof(model.Phone), "رقم الجوال يتجاوز الحد المسموح.");

        if (!string.IsNullOrWhiteSpace(normalizedUserName))
        {
            var normalizedUserNameLower = normalizedUserName.ToLowerInvariant();
            var duplicateUserName = await _context.AdminUsers
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id != user.Id &&
                         x.UserName.ToLower() == normalizedUserNameLower,
                    cancellationToken);

            if (duplicateUserName)
                ModelState.AddModelError(nameof(model.UserName), "اسم المستخدم مستخدم مسبقًا.");
        }

        if (!ModelState.IsValid)
            return await RenderProfileViewAsync(user, model, cancellationToken);

        var oldProfileImage = user.ProfileImagePath;
        string? newProfileImage = null;

        try
        {
            if (profileImage is { Length: > 0 })
                newProfileImage = await _files.UploadAsync(profileImage, "admin-profiles", cancellationToken);

            user.FullName = normalizedFullName;
            user.UserName = normalizedUserName;
            user.Phone = normalizedPhone;

            if (newProfileImage != null)
                user.ProfileImagePath = newProfileImage;

            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (newProfileImage != null)
                _files.Delete(newProfileImage);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            if (newProfileImage != null)
                _files.Delete(newProfileImage);

            _logger.LogWarning(ex, "Rejected admin profile image upload for user {AdminUserId}", user.Id);
            ModelState.AddModelError("", ex.Message);
            return await RenderProfileViewAsync(user, model, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (newProfileImage != null)
                _files.Delete(newProfileImage);

            _logger.LogError(ex, "Failed to update admin profile for user {AdminUserId}", user.Id);
            ModelState.AddModelError("", "تعذر حفظ بيانات الحساب حاليًا. حاول مرة أخرى.");
            return await RenderProfileViewAsync(user, model, cancellationToken);
        }

        if (newProfileImage != null && !string.IsNullOrWhiteSpace(oldProfileImage))
            _files.Delete(oldProfileImage);

        await RefreshSignInAsync(user.Id, cancellationToken);
        TempData["Success"] = "تم تحديث بيانات الحساب بنجاح.";
        return Redirect("/Admin/Profile");
    }

    [HttpPost("/Admin/Profile/RequestEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> RequestEmailChange(
        string? newEmail,
        string? currentPassword,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        var normalizedEmail = (newEmail ?? string.Empty).Trim().ToLowerInvariant();

        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail) ||
            normalizedEmail.Length > 180)
        {
            TempData["EmailChangeError"] = "أدخل بريدًا إلكترونيًا صحيحًا.";
            return Redirect("/Admin/Profile");
        }

        if (string.Equals(user.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["EmailChangeError"] = "البريد الجديد مطابق للبريد الحالي.";
            return Redirect("/Admin/Profile");
        }

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            !AdminPasswordHasher.VerifyPassword(
                currentPassword,
                user.PasswordHash,
                user.PasswordSalt))
        {
            TempData["EmailChangeError"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Admin/Profile");
        }

        var duplicate = await _context.AdminUsers
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != user.Id &&
                     x.Email != null &&
                     x.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (duplicate)
        {
            TempData["EmailChangeError"] = "البريد الإلكتروني مستخدم مسبقًا.";
            return Redirect("/Admin/Profile");
        }

        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    EmailChangePortalType,
                    user.Id,
                    normalizedEmail,
                    user.FullName,
                    RememberMe: false,
                    ReturnUrl: null,
                    IpAddress: GetClientIpAddress()),
                cancellationToken);

            return RedirectToAction(
                nameof(VerifyEmailChange),
                new { challengeId = challenge.ChallengeId });
        }
        catch (OtpRateLimitException ex)
        {
            TempData["EmailChangeError"] = ex.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to send email-change OTP for admin user {AdminUserId}", user.Id);
            TempData["EmailChangeError"] = "تعذر إرسال رمز التحقق إلى البريد الجديد. تحقق من إعدادات البريد ثم حاول مجددًا.";
        }

        return Redirect("/Admin/Profile");
    }

    [HttpGet("/Admin/Profile/VerifyEmailChange")]
    public async Task<IActionResult> VerifyEmailChange(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        var challenge = await GetOwnedEmailChangeChallengeAsync(
            challengeId,
            user.Id,
            requireActive: true,
            cancellationToken);

        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Admin/Profile");
        }

        var info = await _otpService.GetChallengeInfoAsync(
            challenge.ChallengeId,
            cancellationToken);

        if (info == null ||
            !string.Equals(info.PortalType, EmailChangePortalType, StringComparison.Ordinal))
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Admin/Profile");
        }

        return View(
            "~/Areas/Admin/Views/AdminProfile/VerifyEmailChange.cshtml",
            new OtpVerificationViewModel
            {
                ChallengeId = info.ChallengeId,
                MaskedEmail = info.MaskedEmail,
                ExpiresAtUtc = info.ExpiresAtUtc
            });
    }

    [HttpPost("/Admin/Profile/VerifyEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> VerifyEmailChange(
        OtpVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        var challenge = await GetOwnedEmailChangeChallengeAsync(
            model.ChallengeId,
            user.Id,
            requireActive: true,
            cancellationToken);

        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Admin/Profile");
        }

        if (!ModelState.IsValid)
        {
            var info = await _otpService.GetChallengeInfoAsync(model.ChallengeId, cancellationToken);
            model.MaskedEmail = info?.MaskedEmail ?? MaskEmail(challenge.Email);
            model.ExpiresAtUtc = info?.ExpiresAtUtc ?? DateTimeOffset.UtcNow;
            return View("~/Areas/Admin/Views/AdminProfile/VerifyEmailChange.cshtml", model);
        }

        var result = await _otpService.VerifyAsync(
            model.ChallengeId,
            model.Code,
            cancellationToken);

        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId != user.Id ||
            !string.Equals(result.PortalType, EmailChangePortalType, StringComparison.Ordinal))
        {
            AddOtpError(result.Status);

            var info = await _otpService.GetChallengeInfoAsync(model.ChallengeId, cancellationToken);
            model.MaskedEmail = info?.MaskedEmail ?? MaskEmail(challenge.Email);
            model.ExpiresAtUtc = info?.ExpiresAtUtc ?? DateTimeOffset.UtcNow;
            return View("~/Areas/Admin/Views/AdminProfile/VerifyEmailChange.cshtml", model);
        }

        if (user.PasswordChangedAtUtc.HasValue &&
            user.PasswordChangedAtUtc.Value > challenge.CreatedAtUtc)
        {
            TempData["EmailChangeError"] = "تغيرت كلمة المرور بعد إصدار الرمز. أعد طلب تغيير البريد.";
            return Redirect("/Admin/Profile");
        }

        var normalizedEmail = challenge.Email.Trim().ToLowerInvariant();
        var duplicate = await _context.AdminUsers
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != user.Id &&
                     x.Email != null &&
                     x.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (duplicate)
        {
            TempData["EmailChangeError"] = "تعذر اعتماد البريد لأنه أصبح مستخدمًا في حساب آخر. أعد المحاولة ببريد مختلف.";
            return Redirect("/Admin/Profile");
        }

        var oldEmail = user.Email;
        user.Email = normalizedEmail;
        user.SecurityStamp = LoginSecurity.NewSecurityStamp();
        user.UpdatedAt = DateTime.Now;

        await using var emailTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(user.Id, "Admin", DateTime.UtcNow, cancellationToken);
            await emailTransaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogWarning(ex, "Unable to finalize admin email change for user {AdminUserId}", user.Id);
            TempData["EmailChangeError"] =
                "تعذر اعتماد البريد الجديد لأنه قد يكون مستخدمًا في حساب آخر. أعد طلب التغيير.";
            return Redirect("/Admin/Profile");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Unable to invalidate OTP challenges after admin email change for user {AdminUserId}", user.Id);
            TempData["EmailChangeError"] = "تعذر اعتماد تغيير البريد حاليًا. حاول مرة أخرى.";
            return Redirect("/Admin/Profile");
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Admin);

        if (!string.IsNullOrWhiteSpace(oldEmail))
        {
            try
            {
                await _emailSender.SendNotificationAsync(
                    oldEmail,
                    user.FullName,
                    "لوحة تحكم جمعية كفو",
                    "تم تغيير البريد الإلكتروني للحساب",
                    $"تم تغيير البريد الإلكتروني المرتبط بحسابك إلى {MaskEmail(normalizedEmail)}. إذا لم تنفذ هذا الإجراء، تواصل فورًا مع مدير النظام.",
                    actionUrl: null,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to notify old email after admin email change for user {AdminUserId}", user.Id);
            }
        }

        TempData["LoginError"] = "تم التحقق من البريد الجديد وتحديثه بنجاح. سجل الدخول مرة أخرى.";
        return Redirect("/Admin/Login");
    }

    [HttpPost("/Admin/Profile/ResendEmailChangeOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> ResendEmailChangeOtp(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        var challenge = await GetOwnedEmailChangeChallengeAsync(
            challengeId,
            user.Id,
            requireActive: true,
            cancellationToken);

        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Admin/Profile");
        }

        try
        {
            await _otpService.ResendAsync(challenge.ChallengeId, cancellationToken);
            TempData["OtpMessage"] = "تم إرسال رمز تحقق جديد إلى البريد الجديد.";
        }
        catch (OtpRateLimitException ex)
        {
            TempData["OtpError"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["OtpError"] = ex.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to resend email-change OTP for admin user {AdminUserId}", user.Id);
            TempData["OtpError"] = "تعذر إعادة إرسال رمز التحقق حاليًا.";
        }

        return RedirectToAction(
            nameof(VerifyEmailChange),
            new { challengeId = challenge.ChallengeId });
    }

    [HttpPost("/Admin/Profile/ChangePassword")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> ChangePassword(
        AdminChangePasswordViewModel passwordModel,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAdminUserAsync(cancellationToken);

        if (user == null)
            return Redirect("/Admin/Login");

        foreach (var error in PasswordPolicy.Validate(passwordModel.NewPassword))
            ModelState.AddModelError(nameof(passwordModel.NewPassword), error);

        if (!ModelState.IsValid)
        {
            var profile = await BuildProfileModelAsync(user, cancellationToken);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        var validCurrentPassword = AdminPasswordHasher.VerifyPassword(
            passwordModel.CurrentPassword,
            user.PasswordHash,
            user.PasswordSalt);

        if (!validCurrentPassword)
        {
            ModelState.AddModelError(nameof(passwordModel.CurrentPassword), "كلمة المرور الحالية غير صحيحة.");

            var profile = await BuildProfileModelAsync(user, cancellationToken);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        if (AdminPasswordHasher.VerifyPassword(
                passwordModel.NewPassword,
                user.PasswordHash,
                user.PasswordSalt))
        {
            ModelState.AddModelError(nameof(passwordModel.NewPassword), "كلمة المرور الجديدة يجب أن تختلف عن الحالية.");
            var profile = await BuildProfileModelAsync(user, cancellationToken);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        var hashed = AdminPasswordHasher.HashPassword(passwordModel.NewPassword);
        user.PasswordHash = hashed.Hash;
        user.PasswordSalt = hashed.Salt;
        user.SecurityStamp = LoginSecurity.NewSecurityStamp();
        user.MustChangePassword = false;
        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.UpdatedAt = DateTime.Now;

        await using (var passwordTransaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(user.Id, "Admin", DateTime.UtcNow, cancellationToken);
            await passwordTransaction.CommitAsync(cancellationToken);
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Admin);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Admin/Login");
    }

    private async Task<Kafo.Web.Models.AdminUser?> GetCurrentAdminUserAsync(
        CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirstValue("KafoAdminUserId");

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return await _context.AdminUsers
            .FirstOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);
    }

    private async Task<Kafo.Web.Models.LoginOtpChallenge?> GetOwnedEmailChangeChallengeAsync(
        string? challengeId,
        int userId,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeId) ||
            challengeId.Length != 64 ||
            !challengeId.All(Uri.IsHexDigit))
        {
            return null;
        }

        var query = _context.LoginOtpChallenges
            .AsNoTracking()
            .Where(x =>
                x.ChallengeId == challengeId &&
                x.PortalType == EmailChangePortalType &&
                x.AccountId == userId);

        if (requireActive)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.UsedAtUtc == null && x.ExpiresAtUtc > now);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IActionResult> RenderProfileViewAsync(
        Kafo.Web.Models.AdminUser user,
        AdminProfileViewModel postedModel,
        CancellationToken cancellationToken)
    {
        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);

        postedModel.Id = user.Id;
        postedModel.Email = user.Email ?? string.Empty;
        postedModel.ProfileImagePath = user.ProfileImagePath;
        postedModel.IsSuperAdmin = user.IsSuperAdmin;
        postedModel.RoleCode = roleCode;
        postedModel.RoleLabel = AdminRolePolicy.GetLabel(roleCode);
        postedModel.LastLoginAt = user.LastLoginAt;
        postedModel.CreatedAt = user.CreatedAt;
        ViewBag.PasswordModel = new AdminChangePasswordViewModel();

        return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", postedModel);
    }

    private async Task<AdminProfileViewModel> BuildProfileModelAsync(
        Kafo.Web.Models.AdminUser user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);

        return new AdminProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            ProfileImagePath = user.ProfileImagePath,
            IsSuperAdmin = user.IsSuperAdmin,
            RoleCode = roleCode,
            RoleLabel = AdminRolePolicy.GetLabel(roleCode),
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task RefreshSignInAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user == null)
            return;

        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("KafoPortalType", "Admin"),
            new("KafoAdminUserId", user.Id.ToString()),
            new("KafoIsSuperAdmin", user.IsSuperAdmin ? "true" : "false"),
            new("KafoAdminRole", roleCode),
            new("KafoSecurityStamp", user.SecurityStamp),
            new("KafoMustChangePassword", user.MustChangePassword ? "true" : "false"),
            new(ClaimTypes.Role, roleCode)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var identity = new ClaimsIdentity(claims, KafoAuthSchemes.Admin);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            KafoAuthSchemes.Admin,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = false
            });
    }


    private Task<int> InvalidateActiveOtpChallengesAsync(
        int accountId,
        string portalPrefix,
        DateTime usedAtUtc,
        CancellationToken cancellationToken)
        => _context.LoginOtpChallenges
            .Where(x =>
                x.AccountId == accountId &&
                x.PortalType.StartsWith(portalPrefix) &&
                x.UsedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UsedAtUtc, usedAtUtc),
                cancellationToken);

    private string? GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private void AddOtpError(LoginOtpVerificationStatus status)
    {
        var message = status switch
        {
            LoginOtpVerificationStatus.InvalidCode => "رمز التحقق غير صحيح.",
            LoginOtpVerificationStatus.Expired => "انتهت صلاحية رمز التحقق. ابدأ طلب تغيير البريد من جديد.",
            LoginOtpVerificationStatus.Locked => "تم تجاوز عدد المحاولات المسموح. ابدأ طلب تغيير البريد من جديد.",
            _ => "طلب التحقق غير موجود أو لم يعد صالحًا."
        };

        ModelState.AddModelError(nameof(OtpVerificationViewModel.Code), message);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***";

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
