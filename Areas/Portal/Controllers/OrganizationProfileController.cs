using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Models.Organizations;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Authentication;
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
public sealed class OrganizationProfileController : Controller
{
    private const string EmailChangePortalType = "OrganizationEmailChange";
    private const long MaximumProfileRequestBytes = 6L * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILoginOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<OrganizationProfileController> _logger;

    public OrganizationProfileController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILoginOtpService otpService,
        IEmailSender emailSender,
        ILogger<OrganizationProfileController> logger)
    {
        _context = context;
        _files = files;
        _otpService = otpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet("/Portal/Organization/Profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var organization = await GetCurrentOrganizationAsync(asTracking: false, cancellationToken);
        return organization == null
            ? NotFound()
            : View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", organization);
    }

    [HttpPost("/Portal/Organization/Profile")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumProfileRequestBytes)]
    public async Task<IActionResult> Index(
        string? name,
        string? activity,
        string? city,
        string? contactName,
        string? phone,
        IFormFile? logo,
        CancellationToken cancellationToken)
    {
        var organization = await GetCurrentOrganizationAsync(asTracking: true, cancellationToken);
        if (organization == null)
            return NotFound();

        var normalizedName = Normalize(name) ?? string.Empty;
        var normalizedActivity = Normalize(activity);
        var normalizedCity = Normalize(city);
        var normalizedContactName = Normalize(contactName);
        var normalizedPhone = Normalize(phone);

        if (normalizedName.Length == 0)
            ModelState.AddModelError("name", "اسم الجهة مطلوب.");
        else if (normalizedName.Length > 220)
            ModelState.AddModelError("name", "اسم الجهة يتجاوز الحد المسموح.");

        ValidateMaximumLength("activity", normalizedActivity, 180, "النشاط");
        ValidateMaximumLength("city", normalizedCity, 120, "المدينة");
        ValidateMaximumLength("contactName", normalizedContactName, 180, "اسم مسؤول التواصل");
        ValidateMaximumLength("phone", normalizedPhone, 40, "رقم الجوال");

        if (logo is { Length: > 3L * 1024 * 1024 })
            ModelState.AddModelError("logo", "حجم الشعار يجب ألا يتجاوز 3MB.");

        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", organization);

        var oldLogoPath = organization.LogoPath;
        string? newLogoPath = null;

        try
        {
            if (logo is { Length: > 0 })
                newLogoPath = await _files.UploadAsync(logo, "organizations", cancellationToken);

            organization.Name = normalizedName;
            organization.Activity = normalizedActivity;
            organization.City = normalizedCity;
            organization.ContactName = normalizedContactName;
            organization.Phone = normalizedPhone;
            if (newLogoPath != null)
                organization.LogoPath = newLogoPath;
            organization.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (newLogoPath != null)
                _files.Delete(newLogoPath);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            if (newLogoPath != null)
                _files.Delete(newLogoPath);

            _logger.LogWarning(ex, "Rejected organization logo upload for account {OrganizationId}", organization.Id);
            ModelState.AddModelError("logo", ex.Message);
            return View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", organization);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (newLogoPath != null)
                _files.Delete(newLogoPath);

            _logger.LogError(ex, "Failed to update organization profile {OrganizationId}", organization.Id);
            ModelState.AddModelError(string.Empty, "تعذر حفظ بيانات الجهة حاليًا. حاول مرة أخرى.");
            return View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", organization);
        }

        if (newLogoPath != null && !string.IsNullOrWhiteSpace(oldLogoPath))
            _files.Delete(oldLogoPath);

        TempData["Success"] = "تم تحديث بيانات الجهة بنجاح.";
        return Redirect("/Portal/Organization/Profile");
    }

    [HttpPost("/Portal/Organization/Profile/RequestEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> RequestEmailChange(
        string? newEmail,
        string? currentPassword,
        CancellationToken cancellationToken)
    {
        var organization = await GetCurrentOrganizationAsync(asTracking: true, cancellationToken);
        if (organization == null)
            return NotFound();

        var normalizedEmail = (newEmail ?? string.Empty).Trim().ToLowerInvariant();
        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail) || normalizedEmail.Length > 180)
        {
            TempData["EmailChangeError"] = "أدخل بريدًا إلكترونيًا صحيحًا يمكنه استقبال رمز التحقق.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (string.Equals(organization.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["EmailChangeError"] = "البريد الجديد مطابق للبريد الحالي.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            !AdminPasswordHasher.VerifyPassword(
                currentPassword,
                organization.PasswordHash,
                organization.PasswordSalt))
        {
            TempData["EmailChangeError"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (await EmailExistsAsync(normalizedEmail, organization.Id, cancellationToken))
        {
            TempData["EmailChangeError"] = "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.";
            return Redirect("/Portal/Organization/Profile");
        }

        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    EmailChangePortalType,
                    organization.Id,
                    normalizedEmail,
                    organization.Name,
                    RememberMe: false,
                    ReturnUrl: null,
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            return RedirectToAction(nameof(VerifyEmailChange), new { challengeId = challenge.ChallengeId });
        }
        catch (OtpRateLimitException ex)
        {
            TempData["EmailChangeError"] = ex.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unable to send organization email-change OTP for account {OrganizationId}", organization.Id);
            TempData["EmailChangeError"] = "تعذر إرسال رمز التحقق إلى البريد الجديد حاليًا.";
        }

        return Redirect("/Portal/Organization/Profile");
    }

    [HttpGet("/Portal/Organization/Profile/VerifyEmailChange")]
    public async Task<IActionResult> VerifyEmailChange(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var challenge = await GetOwnedEmailChangeChallengeAsync(challengeId, organizationId, cancellationToken);
        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Organization/Profile");
        }

        var info = await _otpService.GetChallengeInfoAsync(challenge.ChallengeId, cancellationToken);
        if (info == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Organization/Profile");
        }

        return View(
            "~/Areas/Portal/Views/OrganizationProfile/VerifyEmailChange.cshtml",
            new OtpVerificationViewModel
            {
                ChallengeId = info.ChallengeId,
                MaskedEmail = info.MaskedEmail,
                ExpiresAtUtc = info.ExpiresAtUtc
            });
    }

    [HttpPost("/Portal/Organization/Profile/VerifyEmailChange")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> VerifyEmailChange(
        OtpVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var organization = await GetCurrentOrganizationAsync(asTracking: true, cancellationToken);
        if (organization == null)
            return NotFound();

        var challenge = await GetOwnedEmailChangeChallengeAsync(model.ChallengeId, organization.Id, cancellationToken);
        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (!ModelState.IsValid)
            return await RenderOtpViewAsync(model, challenge, cancellationToken);

        var result = await _otpService.VerifyAsync(model.ChallengeId, model.Code, cancellationToken);
        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId != organization.Id ||
            !string.Equals(result.PortalType, EmailChangePortalType, StringComparison.Ordinal))
        {
            AddOtpError(result.Status);
            return await RenderOtpViewAsync(model, challenge, cancellationToken);
        }

        if (organization.PasswordChangedAtUtc.HasValue &&
            organization.PasswordChangedAtUtc.Value > challenge.CreatedAtUtc)
        {
            TempData["EmailChangeError"] = "تغيرت كلمة المرور بعد إصدار الرمز. أعد طلب تغيير البريد.";
            return Redirect("/Portal/Organization/Profile");
        }

        var normalizedEmail = challenge.Email.Trim().ToLowerInvariant();
        if (await EmailExistsAsync(normalizedEmail, organization.Id, cancellationToken))
        {
            TempData["EmailChangeError"] = "تعذر اعتماد البريد لأنه أصبح مستخدمًا في حساب آخر.";
            return Redirect("/Portal/Organization/Profile");
        }

        var oldEmail = organization.Email;
        organization.Email = normalizedEmail;
        organization.SecurityStamp = LoginSecurity.NewSecurityStamp();
        organization.UpdatedAt = DateTime.Now;

        await using var emailTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(organization.Id, "Organization", DateTime.UtcNow, cancellationToken);
            await emailTransaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogWarning(ex, "Unable to finalize organization email change for account {OrganizationId}", organization.Id);
            TempData["EmailChangeError"] =
                "تعذر اعتماد البريد الجديد لأنه قد يكون مستخدمًا في حساب آخر. أعد طلب التغيير.";
            return Redirect("/Portal/Organization/Profile");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await emailTransaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Unable to invalidate OTP challenges after organization email change for account {OrganizationId}", organization.Id);
            TempData["EmailChangeError"] = "تعذر اعتماد تغيير البريد حاليًا. حاول مرة أخرى.";
            return Redirect("/Portal/Organization/Profile");
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        await NotifyOldEmailAsync(
            oldEmail,
            organization.Name,
            normalizedEmail,
            organization.Id);

        TempData["LoginError"] = "تم التحقق من البريد الجديد وتحديثه بنجاح. سجل الدخول مرة أخرى.";
        return Redirect("/Portal/Login");
    }

    [HttpPost("/Portal/Organization/Profile/ResendEmailChangeOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> ResendEmailChangeOtp(
        string? challengeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var challenge = await GetOwnedEmailChangeChallengeAsync(challengeId, organizationId, cancellationToken);
        if (challenge == null)
        {
            TempData["EmailChangeError"] = "طلب تغيير البريد غير موجود أو انتهت صلاحيته.";
            return Redirect("/Portal/Organization/Profile");
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
            _logger.LogError(ex, "Unable to resend organization email-change OTP for account {OrganizationId}", organizationId);
            TempData["OtpError"] = "تعذر إعادة إرسال رمز التحقق حاليًا.";
        }

        return RedirectToAction(nameof(VerifyEmailChange), new { challengeId });
    }

    [HttpPost("/Portal/Organization/Profile/Password")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> ChangePassword(
        PortalChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        var organization = await GetCurrentOrganizationAsync(asTracking: true, cancellationToken);
        if (organization == null)
            return NotFound();

        foreach (var error in PasswordPolicy.Validate(model.NewPassword))
            ModelState.AddModelError(nameof(model.NewPassword), error);

        if (!ModelState.IsValid)
        {
            TempData["Error"] = JoinModelErrors();
            return Redirect("/Portal/Organization/Profile");
        }

        if (!AdminPasswordHasher.VerifyPassword(
                model.CurrentPassword,
                organization.PasswordHash,
                organization.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (AdminPasswordHasher.VerifyPassword(
                model.NewPassword,
                organization.PasswordHash,
                organization.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الجديدة يجب أن تختلف عن كلمة المرور الحالية.";
            return Redirect("/Portal/Organization/Profile");
        }

        var hashed = AdminPasswordHasher.HashPassword(model.NewPassword);
        organization.PasswordHash = hashed.Hash;
        organization.PasswordSalt = hashed.Salt;
        organization.SecurityStamp = LoginSecurity.NewSecurityStamp();
        organization.MustChangePassword = false;
        organization.AccessFailedCount = 0;
        organization.LockoutEndUtc = null;
        organization.PasswordChangedAtUtc = DateTime.UtcNow;
        organization.UpdatedAt = DateTime.Now;

        await using (var passwordTransaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateActiveOtpChallengesAsync(organization.Id, "Organization", DateTime.UtcNow, cancellationToken);
            await passwordTransaction.CommitAsync(cancellationToken);
        }

        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Portal/Login");
    }

    private async Task<OrganizationAccount?> GetCurrentOrganizationAsync(
        bool asTracking,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return null;

        var query = _context.OrganizationAccounts.Where(x => x.Id == organizationId && x.IsActive);
        if (!asTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<LoginOtpChallenge?> GetOwnedEmailChangeChallengeAsync(
        string? challengeId,
        int organizationId,
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
                     x.AccountId == organizationId &&
                     x.UsedAtUtc == null &&
                     x.ExpiresAtUtc > now,
                cancellationToken);
    }

    private async Task<bool> EmailExistsAsync(
        string normalizedEmail,
        int organizationId,
        CancellationToken cancellationToken)
    {
        return await _context.OrganizationAccounts.AsNoTracking().AnyAsync(
                   x => x.Id != organizationId && x.Email != null && x.Email.ToLower() == normalizedEmail,
                   cancellationToken) ||
               await _context.DonorAccounts.AsNoTracking().AnyAsync(
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
        return View("~/Areas/Portal/Views/OrganizationProfile/VerifyEmailChange.cshtml", model);
    }

    private async Task NotifyOldEmailAsync(
        string? oldEmail,
        string displayName,
        string newEmail,
        int organizationId)
    {
        if (string.IsNullOrWhiteSpace(oldEmail))
            return;

        try
        {
            await _emailSender.SendNotificationAsync(
                oldEmail,
                displayName,
                "بوابة الجهات والشركات",
                "تم تغيير البريد الإلكتروني للحساب",
                $"تم تغيير البريد المرتبط بحسابك إلى {MaskEmail(newEmail)}. إذا لم تنفذ هذا الإجراء، تواصل فورًا مع إدارة الجمعية.",
                actionUrl: null,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to notify old email for organization account {OrganizationId}", organizationId);
        }
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

    private bool TryGetOrganizationId(out int organizationId)
    {
        var value = User.FindFirstValue("KafoOrganizationUserId") ??
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out organizationId) && organizationId > 0;
    }

    private void ValidateMaximumLength(string key, string? value, int maximum, string displayName)
    {
        if (value is { Length: > 0 } && value.Length > maximum)
            ModelState.AddModelError(key, $"{displayName} يتجاوز الحد المسموح.");
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
