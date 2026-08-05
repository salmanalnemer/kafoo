using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Authentication;
using Kafo.Web.ViewModels.Donor;
using Kafo.Web.ViewModels.Portal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Portal)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DonorProfileController : Controller
{
    private const string EmailChangePortalType = "DonorEmailChange";

    private static readonly HashSet<string> AllowedDonorTypes =
        new(["فرد", "جهة", "شركة", "مؤسسة مانحة"], StringComparer.Ordinal);

    private readonly ApplicationDbContext _context;
    private readonly ILoginOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DonorProfileController> _logger;

    public DonorProfileController(
        ApplicationDbContext context,
        ILoginOtpService otpService,
        IEmailSender emailSender,
        ILogger<DonorProfileController> logger)
    {
        _context = context;
        _otpService = otpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet("/Portal/Donor/Profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var donor = await GetCurrentDonorAsync(asTracking: false, cancellationToken);
        return donor == null
            ? NotFound()
            : View("~/Areas/Portal/Views/DonorProfile/Index.cshtml", ToViewModel(donor));
    }

    [HttpPost("/Portal/Donor/Profile")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Index(
        [Bind("FullName,DonorType,OrganizationName,Phone")]
        DonorProfileViewModel model,
        CancellationToken cancellationToken)
    {
        var donor = await GetCurrentDonorAsync(asTracking: true, cancellationToken);
        if (donor == null)
            return NotFound();

        // البريد له مسار مستقل يتطلب كلمة المرور الحالية وOTP.
        ModelState.Remove(nameof(model.Email));

        model.FullName = (model.FullName ?? string.Empty).Trim();
        model.DonorType = string.IsNullOrWhiteSpace(model.DonorType)
            ? "فرد"
            : model.DonorType.Trim();
        model.OrganizationName = Normalize(model.OrganizationName);
        model.Phone = Normalize(model.Phone);

        if (!AllowedDonorTypes.Contains(model.DonorType))
            ModelState.AddModelError(nameof(model.DonorType), "نوع الداعم غير صالح.");

        if (string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError(nameof(model.FullName), "اسم الداعم مطلوب.");

        if (model.Phone is { Length: > 40 })
            ModelState.AddModelError(nameof(model.Phone), "رقم الجوال يتجاوز الحد المسموح.");

        if (!ModelState.IsValid)
        {
            PopulateSystemFields(model, donor);
            return View("~/Areas/Portal/Views/DonorProfile/Index.cshtml", model);
        }

        donor.FullName = model.FullName;
        donor.DonorType = model.DonorType;
        donor.OrganizationName = model.OrganizationName;
        donor.Phone = model.Phone;
        donor.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم تحديث بيانات الحساب بنجاح.";
        return Redirect("/Portal/Donor/Profile");
    }

    [HttpPost("/Portal/Donor/Profile/RequestEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> RequestEmailChange(
        string? newEmail,
        string? currentPassword,
        CancellationToken cancellationToken)
    {
        var donor = await GetCurrentDonorAsync(asTracking: true, cancellationToken);
        if (donor == null)
            return NotFound();

        var normalizedEmail = (newEmail ?? string.Empty).Trim().ToLowerInvariant();

        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail) || normalizedEmail.Length > 180)
        {
            TempData["EmailChangeError"] = "أدخل بريدًا إلكترونيًا صحيحًا يمكنه استقبال رمز التحقق.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (string.Equals(donor.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["EmailChangeError"] = "البريد الجديد مطابق للبريد الحالي.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            !AdminPasswordHasher.VerifyPassword(
                currentPassword,
                donor.PasswordHash,
                donor.PasswordSalt))
        {
            TempData["EmailChangeError"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (await EmailExistsAsync(normalizedEmail, donor.Id, cancellationToken))
        {
            TempData["EmailChangeError"] = "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.";
            return Redirect("/Portal/Donor/Profile");
        }

        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    EmailChangePortalType,
                    donor.Id,
                    normalizedEmail,
                    donor.FullName,
                    RememberMe: false,
                    ReturnUrl: null,
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()),
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
            _logger.LogError(ex, "Unable to send donor email-change OTP for account {DonorId}", donor.Id);
            TempData["EmailChangeError"] = "تعذر إرسال رمز التحقق إلى البريد الجديد حاليًا.";
        }

        return Redirect("/Portal/Donor/Profile");
    }

    [HttpGet("/Portal/Donor/Profile/VerifyEmailChange")]
    public async Task<IActionResult> VerifyEmailChange(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
            return Forbid();

        var challenge = await GetOwnedEmailChangeChallengeAsync(
            challengeId,
            donorId,
            cancellationToken);

        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Donor/Profile");
        }

        var info = await _otpService.GetChallengeInfoAsync(challenge.ChallengeId, cancellationToken);
        if (info == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Donor/Profile");
        }

        return View(
            "~/Areas/Portal/Views/DonorProfile/VerifyEmailChange.cshtml",
            new OtpVerificationViewModel
            {
                ChallengeId = info.ChallengeId,
                MaskedEmail = info.MaskedEmail,
                ExpiresAtUtc = info.ExpiresAtUtc
            });
    }

    [HttpPost("/Portal/Donor/Profile/VerifyEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> VerifyEmailChange(
        OtpVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var donor = await GetCurrentDonorAsync(asTracking: true, cancellationToken);
        if (donor == null)
            return NotFound();

        var challenge = await GetOwnedEmailChangeChallengeAsync(
            model.ChallengeId,
            donor.Id,
            cancellationToken);

        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (!ModelState.IsValid)
            return await RenderOtpViewAsync(model, challenge, cancellationToken);

        var result = await _otpService.VerifyAsync(model.ChallengeId, model.Code, cancellationToken);
        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId != donor.Id ||
            !string.Equals(result.PortalType, EmailChangePortalType, StringComparison.Ordinal))
        {
            AddOtpError(result.Status);
            return await RenderOtpViewAsync(model, challenge, cancellationToken);
        }

        if (donor.PasswordChangedAtUtc.HasValue &&
            donor.PasswordChangedAtUtc.Value > challenge.CreatedAtUtc)
        {
            TempData["EmailChangeError"] = "تغيرت كلمة المرور بعد إصدار الرمز. أعد طلب تغيير البريد.";
            return Redirect("/Portal/Donor/Profile");
        }

        var normalizedEmail = challenge.Email.Trim().ToLowerInvariant();
        if (await EmailExistsAsync(normalizedEmail, donor.Id, cancellationToken))
        {
            TempData["EmailChangeError"] = "تعذر اعتماد البريد لأنه أصبح مستخدمًا في حساب آخر.";
            return Redirect("/Portal/Donor/Profile");
        }

        var oldEmail = donor.Email;
        donor.Email = normalizedEmail;
        donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
        donor.UpdatedAt = DateTime.Now;

        await using var emailTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(donor.Id, "Donor", DateTime.UtcNow, cancellationToken);
            await emailTransaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogWarning(ex, "Unable to finalize donor email change for account {DonorId}", donor.Id);
            TempData["EmailChangeError"] =
                "تعذر اعتماد البريد الجديد لأنه قد يكون مستخدمًا في حساب آخر. أعد طلب التغيير.";
            return Redirect("/Portal/Donor/Profile");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Unable to invalidate OTP challenges after donor email change for account {DonorId}", donor.Id);
            TempData["EmailChangeError"] = "تعذر اعتماد تغيير البريد حاليًا. حاول مرة أخرى.";
            return Redirect("/Portal/Donor/Profile");
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        await NotifyOldEmailAsync(oldEmail, donor.FullName, normalizedEmail, donor.Id);

        TempData["LoginError"] = "تم التحقق من البريد الجديد وتحديثه بنجاح. سجل الدخول مرة أخرى.";
        return Redirect("/Portal/Login");
    }

    [HttpPost("/Portal/Donor/Profile/ResendEmailChangeOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> ResendEmailChangeOtp(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
            return Forbid();

        var challenge = await GetOwnedEmailChangeChallengeAsync(challengeId, donorId, cancellationToken);
        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Donor/Profile");
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unable to resend donor email-change OTP for account {DonorId}", donorId);
            TempData["OtpError"] = "تعذر إعادة إرسال رمز التحقق حاليًا.";
        }

        return RedirectToAction(nameof(VerifyEmailChange), new { challengeId });
    }

    [HttpPost("/Portal/Donor/Profile/Password")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> ChangePassword(
        PortalChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        var donor = await GetCurrentDonorAsync(asTracking: true, cancellationToken);
        if (donor == null)
            return NotFound();

        foreach (var error in PasswordPolicy.Validate(model.NewPassword))
            ModelState.AddModelError(nameof(model.NewPassword), error);

        if (!ModelState.IsValid)
        {
            TempData["Error"] = JoinModelErrors();
            return Redirect("/Portal/Donor/Profile");
        }

        if (!AdminPasswordHasher.VerifyPassword(
                model.CurrentPassword,
                donor.PasswordHash,
                donor.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (AdminPasswordHasher.VerifyPassword(
                model.NewPassword,
                donor.PasswordHash,
                donor.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الجديدة يجب أن تختلف عن كلمة المرور الحالية.";
            return Redirect("/Portal/Donor/Profile");
        }

        var hashed = AdminPasswordHasher.HashPassword(model.NewPassword);
        donor.PasswordHash = hashed.Hash;
        donor.PasswordSalt = hashed.Salt;
        donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
        donor.MustChangePassword = false;
        donor.AccessFailedCount = 0;
        donor.LockoutEndUtc = null;
        donor.PasswordChangedAtUtc = DateTime.UtcNow;
        donor.UpdatedAt = DateTime.Now;

        await using (var passwordTransaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(donor.Id, "Donor", DateTime.UtcNow, cancellationToken);
            await passwordTransaction.CommitAsync(cancellationToken);
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Portal/Login");
    }

    private async Task<Kafo.Web.Models.Donors.DonorAccount?> GetCurrentDonorAsync(
        bool asTracking,
        CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
            return null;

        var query = _context.DonorAccounts.Where(x => x.Id == donorId && x.IsActive);
        if (!asTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<LoginOtpChallenge?> GetOwnedEmailChangeChallengeAsync(
        string? challengeId,
        int donorId,
        CancellationToken cancellationToken)
    {
        if (!IsValidChallengeId(challengeId))
            return null;

        var now = DateTime.UtcNow;
        return await _context.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ChallengeId == challengeId &&
                     x.PortalType == EmailChangePortalType &&
                     x.AccountId == donorId &&
                     x.UsedAtUtc == null &&
                     x.ExpiresAtUtc > now,
                cancellationToken);
    }

    private async Task<bool> EmailExistsAsync(
        string normalizedEmail,
        int donorId,
        CancellationToken cancellationToken)
    {
        return await _context.DonorAccounts.AsNoTracking().AnyAsync(
                   x => x.Id != donorId && x.Email != null && x.Email.ToLower() == normalizedEmail,
                   cancellationToken) ||
               await _context.OrganizationAccounts.AsNoTracking().AnyAsync(
                   x => x.Email != null && x.Email.ToLower() == normalizedEmail,
                   cancellationToken);
    }

    private async Task<IActionResult> RenderOtpViewAsync(
        OtpVerificationViewModel model,
        LoginOtpChallenge challenge,
        CancellationToken cancellationToken)
    {
        var info = await _otpService.GetChallengeInfoAsync(model.ChallengeId, cancellationToken);
        model.MaskedEmail = info?.MaskedEmail ?? MaskEmail(challenge.Email);
        model.ExpiresAtUtc = info?.ExpiresAtUtc ?? DateTimeOffset.UtcNow;
        return View("~/Areas/Portal/Views/DonorProfile/VerifyEmailChange.cshtml", model);
    }

    private async Task NotifyOldEmailAsync(
        string? oldEmail,
        string displayName,
        string newEmail,
        int donorId)
    {
        if (string.IsNullOrWhiteSpace(oldEmail))
            return;

        try
        {
            await _emailSender.SendNotificationAsync(
                oldEmail,
                displayName,
                "بوابة الداعمين",
                "تم تغيير البريد الإلكتروني للحساب",
                $"تم تغيير البريد المرتبط بحسابك إلى {MaskEmail(newEmail)}. إذا لم تنفذ هذا الإجراء، تواصل فورًا مع إدارة الجمعية.",
                actionUrl: null,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to notify old email for donor account {DonorId}", donorId);
        }
    }

    private static DonorProfileViewModel ToViewModel(Kafo.Web.Models.Donors.DonorAccount donor)
        => new()
        {
            Id = donor.Id,
            FullName = donor.FullName,
            DonorType = donor.DonorType,
            OrganizationName = donor.OrganizationName,
            Email = donor.Email ?? string.Empty,
            Phone = donor.Phone,
            LastLoginAt = donor.LastLoginAt,
            CreatedAt = donor.CreatedAt
        };

    private static void PopulateSystemFields(
        DonorProfileViewModel model,
        Kafo.Web.Models.Donors.DonorAccount donor)
    {
        model.Id = donor.Id;
        model.Email = donor.Email ?? string.Empty;
        model.LastLoginAt = donor.LastLoginAt;
        model.CreatedAt = donor.CreatedAt;
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

    private bool TryGetDonorId(out int donorId)
    {
        var value = User.FindFirstValue("KafoDonorUserId") ??
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out donorId) && donorId > 0;
    }

    private string JoinModelErrors()
        => string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

    private void AddOtpError(LoginOtpVerificationStatus status)
    {
        var message = status switch
        {
            LoginOtpVerificationStatus.InvalidCode => "رمز التحقق غير صحيح.",
            LoginOtpVerificationStatus.Expired => "انتهت صلاحية الرمز. ابدأ طلب تغيير البريد من جديد.",
            LoginOtpVerificationStatus.Locked => "تم تجاوز عدد المحاولات. ابدأ طلب تغيير البريد من جديد.",
            _ => "طلب التحقق غير موجود أو لم يعد صالحًا."
        };

        ModelState.AddModelError(nameof(OtpVerificationViewModel.Code), message);
    }

    private static bool IsValidChallengeId(string? challengeId)
        => !string.IsNullOrWhiteSpace(challengeId) &&
           challengeId.Length == 64 &&
           challengeId.All(Uri.IsHexDigit);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
