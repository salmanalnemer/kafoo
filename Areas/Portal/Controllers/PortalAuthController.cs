using System.Security.Claims;
using Kafo.Web.Configuration;
using Kafo.Web.Data;
using Kafo.Web.Models.Donors;
using Kafo.Web.Models.Organizations;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Authentication;
using Kafo.Web.ViewModels.Portal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PortalAuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILoginOtpService _otpService;
    private readonly ISecurityAuditService _audit;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<PortalAuthController> _logger;

    public PortalAuthController(
        ApplicationDbContext context,
        ILoginOtpService otpService,
        ISecurityAuditService audit,
        IOptions<SecurityOptions> securityOptions,
        ILogger<PortalAuthController> logger)
    {
        _context = context;
        _otpService = otpService;
        _audit = audit;
        _securityOptions = securityOptions.Value;
        _logger = logger;
    }

    [HttpGet("/Portal/Login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var authResult = await HttpContext.AuthenticateAsync(KafoAuthSchemes.Portal);
        if (authResult.Succeeded && authResult.Principal != null)
            return Redirect(GetHomePath(authResult.Principal.FindFirst("KafoPortalType")?.Value));

        return View("~/Areas/Portal/Views/Auth/Login.cshtml", new PortalLoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("/Portal/Login")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PortalLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);

        var email = model.Email.Trim().ToLowerInvariant();
        var donor = await _context.DonorAccounts
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email, cancellationToken);
        var organization = await _context.OrganizationAccounts
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email, cancellationToken);

        var donorValid = VerifyAccount(
            donor?.IsActive == true,
            donor?.LockoutEndUtc,
            model.Password,
            donor?.PasswordHash,
            donor?.PasswordSalt,
            out var donorNeedsRehash);
        var organizationValid = VerifyAccount(
            organization?.IsActive == true,
            organization?.LockoutEndUtc,
            model.Password,
            organization?.PasswordHash,
            organization?.PasswordSalt,
            out var organizationNeedsRehash);

        if (donorValid && organizationValid)
        {
            _logger.LogWarning(
                "Duplicate unified portal email detected for donor {DonorId} and organization {OrganizationId}",
                donor!.Id,
                organization!.Id);
            await _audit.WriteAsync(
                HttpContext,
                "PortalDuplicateEmail",
                "The same normalized email is associated with donor and organization accounts.",
                success: false,
                severity: "Critical",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty,
                "تعذر تحديد الحساب المرتبط بالبريد. تواصل مع إدارة النظام.");
            return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);
        }

        if (donorValid && donor != null)
        {
            ResetAndUpgrade(donor, model.Password, donorNeedsRehash);
            await _context.SaveChangesAsync(cancellationToken);
            return await SendOtpAndRedirectAsync(
                "Donor", donor.Id, email, donor.FullName, model, cancellationToken);
        }

        if (organizationValid && organization != null)
        {
            ResetAndUpgrade(organization, model.Password, organizationNeedsRehash);
            await _context.SaveChangesAsync(cancellationToken);
            return await SendOtpAndRedirectAsync(
                "Organization", organization.Id, email, organization.Name, model, cancellationToken);
        }

        var retryAfter = MaxDuration(
            LoginSecurity.GetRemainingLockout(donor?.LockoutEndUtc),
            LoginSecurity.GetRemainingLockout(organization?.LockoutEndUtc));

        if (donor is { IsActive: true } && !LoginSecurity.IsLocked(donor.LockoutEndUtc))
            retryAfter = MaxDuration(retryAfter, RegisterFailure(donor));

        if (organization is { IsActive: true } &&
            !LoginSecurity.IsLocked(organization.LockoutEndUtc))
        {
            retryAfter = MaxDuration(retryAfter, RegisterFailure(organization));
        }

        if (donor != null || organization != null)
            await _context.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            HttpContext,
            "PortalLoginFailed",
            "Unified portal login credentials were rejected.",
            success: false,
            severity: "Warning",
            actorType: donor != null ? "Donor" : organization != null ? "Organization" : null,
            actorId: donor?.Id.ToString() ?? organization?.Id.ToString(),
            cancellationToken: cancellationToken);

        ModelState.AddModelError(
            string.Empty,
            BuildLoginFailureMessage(retryAfter));

        return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);
    }

    [HttpGet("/Portal/VerifyOtp")]
    public async Task<IActionResult> VerifyOtp(string challengeId, CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(challengeId, cancellationToken);
        if (challenge == null || !IsExternalPortalType(challenge.PortalType))
        {
            TempData["LoginError"] = "انتهت صلاحية طلب التحقق. سجل الدخول مرة أخرى.";
            return Redirect("/Portal/Login");
        }

        return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", new OtpVerificationViewModel
        {
            ChallengeId = challenge.ChallengeId,
            MaskedEmail = challenge.MaskedEmail,
            ExpiresAtUtc = challenge.ExpiresAtUtc
        });
    }

    [HttpPost("/Portal/VerifyOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(
        OtpVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(model.ChallengeId, cancellationToken);
        if (challenge != null)
        {
            model.MaskedEmail = challenge.MaskedEmail;
            model.ExpiresAtUtc = challenge.ExpiresAtUtc;
        }

        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);

        var result = await _otpService.VerifyAsync(model.ChallengeId, model.Code, cancellationToken);
        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId == null ||
            string.IsNullOrWhiteSpace(result.PortalType))
        {
            await _audit.WriteAsync(
                HttpContext,
                "PortalOtpFailed",
                $"Portal OTP verification failed with status {result.Status}.",
                success: false,
                severity: "Warning",
                actorType: result.PortalType,
                actorId: result.AccountId?.ToString(),
                cancellationToken: cancellationToken);
            AddOtpError(result.Status);
            return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);
        }

        if (string.Equals(result.PortalType, "Donor", StringComparison.OrdinalIgnoreCase))
        {
            var donor = await _context.DonorAccounts
                .FirstOrDefaultAsync(x => x.Id == result.AccountId.Value && x.IsActive, cancellationToken);
            if (donor == null || LoginSecurity.IsLocked(donor.LockoutEndUtc))
            {
                ModelState.AddModelError(string.Empty, "الحساب غير موجود أو غير مفعل.");
                return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);
            }

            donor.LastLoginAt = DateTime.Now;
            donor.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
            await SignInDonorAsync(donor, result.RememberMe);
            await WriteSuccessAuditAsync("Donor", donor.Id, cancellationToken);
            return donor.MustChangePassword
                ? Redirect("/Portal/Donor/Profile#change-password")
                : RedirectAfterLogin(result.ReturnUrl, "Donor");
        }

        if (string.Equals(result.PortalType, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            var organization = await _context.OrganizationAccounts
                .FirstOrDefaultAsync(x => x.Id == result.AccountId.Value && x.IsActive, cancellationToken);
            if (organization == null || LoginSecurity.IsLocked(organization.LockoutEndUtc))
            {
                ModelState.AddModelError(string.Empty, "الحساب غير موجود أو غير مفعل.");
                return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);
            }

            organization.LastLoginAt = DateTime.Now;
            organization.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
            await SignInOrganizationAsync(organization, result.RememberMe);
            await WriteSuccessAuditAsync("Organization", organization.Id, cancellationToken);
            return organization.MustChangePassword
                ? Redirect("/Portal/Organization/Profile#change-password")
                : RedirectAfterLogin(result.ReturnUrl, "Organization");
        }

        ModelState.AddModelError(string.Empty, "نوع الحساب غير صالح.");
        return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);
    }

    [HttpPost("/Portal/ResendOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string challengeId, CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(challengeId, cancellationToken);
        if (challenge == null || !IsExternalPortalType(challenge.PortalType))
        {
            TempData["LoginError"] = "انتهت صلاحية طلب التحقق. سجل الدخول مرة أخرى.";
            return Redirect("/Portal/Login");
        }

        try
        {
            await _otpService.ResendAsync(challengeId, cancellationToken);
            TempData["OtpMessage"] = "تم إرسال رمز تحقق جديد إلى بريدك الإلكتروني.";
        }
        catch (OtpRateLimitException ex)
        {
            TempData["OtpError"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to resend portal OTP for challenge {ChallengeId}", challengeId);
            TempData["OtpError"] = "تعذر إعادة إرسال الرمز. حاول مرة أخرى.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { challengeId });
    }

    [HttpPost("/Portal/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var portalType = User.FindFirstValue("KafoPortalType");
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);
        await HttpContext.SignOutAsync(KafoAuthSchemes.Donor);
        await HttpContext.SignOutAsync(KafoAuthSchemes.Organization);
        await _audit.WriteAsync(
            HttpContext,
            "PortalLogout",
            "Portal account signed out.",
            success: true,
            actorType: portalType,
            actorId: accountId,
            cancellationToken: cancellationToken);
        return Redirect("/Portal/Login");
    }

    private async Task<IActionResult> SendOtpAndRedirectAsync(
        string portalType,
        int accountId,
        string email,
        string displayName,
        PortalLoginViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    portalType,
                    accountId,
                    email,
                    displayName,
                    model.RememberMe,
                    model.ReturnUrl,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            return RedirectToAction(nameof(VerifyOtp), new { challengeId = challenge.ChallengeId });
        }
        catch (OtpRateLimitException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unable to send portal OTP for {PortalType} account {AccountId}",
                portalType,
                accountId);
            ModelState.AddModelError(string.Empty,
                "تعذر إرسال رمز التحقق. تأكد من إعدادات البريد الإلكتروني ثم حاول مجددًا.");
        }

        return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);
    }

    private async Task SignInDonorAsync(DonorAccount donor, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, donor.Id.ToString()),
            new(ClaimTypes.Name, donor.FullName),
            new("KafoPortalType", "Donor"),
            new("KafoDonorUserId", donor.Id.ToString()),
            new("KafoSecurityStamp", donor.SecurityStamp),
            new("KafoMustChangePassword", donor.MustChangePassword ? "true" : "false")
        };
        if (!string.IsNullOrWhiteSpace(donor.Email))
            claims.Add(new Claim(ClaimTypes.Email, donor.Email));
        await SignInAsync(claims, rememberMe);
    }

    private async Task SignInOrganizationAsync(OrganizationAccount organization, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, organization.Id.ToString()),
            new(ClaimTypes.Name, organization.Name),
            new("KafoPortalType", "Organization"),
            new("KafoOrganizationUserId", organization.Id.ToString()),
            new("KafoSecurityStamp", organization.SecurityStamp),
            new("KafoMustChangePassword", organization.MustChangePassword ? "true" : "false")
        };
        if (!string.IsNullOrWhiteSpace(organization.Email))
            claims.Add(new Claim(ClaimTypes.Email, organization.Email));
        await SignInAsync(claims, rememberMe);
    }

    private async Task SignInAsync(List<Claim> claims, bool rememberMe)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, KafoAuthSchemes.Portal));
        await HttpContext.SignInAsync(
            KafoAuthSchemes.Portal,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = false
            });
    }

    private bool VerifyAccount(
        bool isActive,
        DateTime? lockoutEndUtc,
        string password,
        string? hash,
        string? salt,
        out bool needsRehash)
    {
        needsRehash = false;
        if (!isActive || LoginSecurity.IsLocked(lockoutEndUtc) ||
            string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            AdminPasswordHasher.VerifyDummy(password);
            return false;
        }
        return AdminPasswordHasher.VerifyPassword(password, hash, salt, out needsRehash);
    }

    private TimeSpan? RegisterFailure(DonorAccount account)
    {
        var count = account.AccessFailedCount;
        var lockout = account.LockoutEndUtc;
        var duration = LoginSecurity.RegisterFailure(
            ref count,
            ref lockout,
            _securityOptions);

        account.AccessFailedCount = count;
        account.LockoutEndUtc = lockout;
        account.UpdatedAt = DateTime.Now;

        return duration;
    }

    private TimeSpan? RegisterFailure(OrganizationAccount account)
    {
        var count = account.AccessFailedCount;
        var lockout = account.LockoutEndUtc;
        var duration = LoginSecurity.RegisterFailure(
            ref count,
            ref lockout,
            _securityOptions);

        account.AccessFailedCount = count;
        account.LockoutEndUtc = lockout;
        account.UpdatedAt = DateTime.Now;

        return duration;
    }

    private static void ResetAndUpgrade(DonorAccount account, string password, bool needsRehash)
    {
        account.AccessFailedCount = 0;
        account.LockoutEndUtc = null;
        if (needsRehash)
        {
            var upgraded = AdminPasswordHasher.HashPassword(password);
            account.PasswordHash = upgraded.Hash;
            account.PasswordSalt = upgraded.Salt;
            account.PasswordChangedAtUtc ??= DateTime.UtcNow;
        }
        account.SecurityStamp = string.IsNullOrWhiteSpace(account.SecurityStamp)
            ? LoginSecurity.NewSecurityStamp()
            : account.SecurityStamp;
        account.UpdatedAt = DateTime.Now;
    }

    private static void ResetAndUpgrade(OrganizationAccount account, string password, bool needsRehash)
    {
        account.AccessFailedCount = 0;
        account.LockoutEndUtc = null;
        if (needsRehash)
        {
            var upgraded = AdminPasswordHasher.HashPassword(password);
            account.PasswordHash = upgraded.Hash;
            account.PasswordSalt = upgraded.Salt;
            account.PasswordChangedAtUtc ??= DateTime.UtcNow;
        }
        account.SecurityStamp = string.IsNullOrWhiteSpace(account.SecurityStamp)
            ? LoginSecurity.NewSecurityStamp()
            : account.SecurityStamp;
        account.UpdatedAt = DateTime.Now;
    }

    private async Task WriteSuccessAuditAsync(string portalType, int accountId, CancellationToken cancellationToken)
        => await _audit.WriteAsync(
            HttpContext,
            "PortalLoginSucceeded",
            "Portal account completed password and OTP authentication.",
            success: true,
            actorType: portalType,
            actorId: accountId.ToString(),
            cancellationToken: cancellationToken);

    private IActionResult RedirectAfterLogin(string? returnUrl, string portalType)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            if (portalType == "Donor" && returnUrl.StartsWith("/Portal/Donor", StringComparison.OrdinalIgnoreCase))
                return Redirect(returnUrl);
            if (portalType == "Organization" && returnUrl.StartsWith("/Portal/Organization", StringComparison.OrdinalIgnoreCase))
                return Redirect(returnUrl);
        }
        return Redirect(GetHomePath(portalType));
    }

    private static string GetHomePath(string? portalType)
        => string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase)
            ? "/Portal/Donor/Dashboard"
            : string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase)
                ? "/Portal/Organization/Dashboard"
                : "/Portal/Login";

    private static bool IsExternalPortalType(string? portalType)
        => string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase);

    private static string BuildLoginFailureMessage(TimeSpan? retryAfter)
    {
        if (!retryAfter.HasValue || retryAfter.Value <= TimeSpan.Zero)
        {
            return "تعذر تسجيل الدخول. تحقق من البريد الإلكتروني وكلمة المرور.";
        }

        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.Value.TotalMinutes));
        var durationText = minutes switch
        {
            1 => "دقيقة واحدة",
            2 => "دقيقتين",
            _ => $"{minutes} دقائق"
        };

        return $"تم تعليق محاولة تسجيل الدخول مؤقتًا لحماية الحساب. حاول مجددًا بعد {durationText}.";
    }

    private static TimeSpan? MaxDuration(TimeSpan? first, TimeSpan? second)
    {
        if (!first.HasValue)
            return second;

        if (!second.HasValue)
            return first;

        return first.Value >= second.Value ? first : second;
    }

    private void AddOtpError(LoginOtpVerificationStatus status)
    {
        var message = status switch
        {
            LoginOtpVerificationStatus.InvalidCode => "رمز التحقق غير صحيح.",
            LoginOtpVerificationStatus.Expired => "انتهت صلاحية رمز التحقق. سجل الدخول مرة أخرى.",
            LoginOtpVerificationStatus.Locked => "تم تجاوز عدد المحاولات المسموح. سجل الدخول مرة أخرى.",
            _ => "طلب التحقق غير موجود أو انتهت صلاحيته. سجل الدخول مرة أخرى."
        };
        ModelState.AddModelError(string.Empty, message);
    }
}
