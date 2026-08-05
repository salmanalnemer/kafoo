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
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Login(PortalLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);

        var email = model.Email.Trim().ToLowerInvariant();
        var donorSnapshot = await _context.DonorAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Email != null && x.Email.ToLower() == email,
                cancellationToken);
        var organizationSnapshot = await _context.OrganizationAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Email != null && x.Email.ToLower() == email,
                cancellationToken);

        var donorValid = VerifyAccount(
            donorSnapshot?.IsActive == true,
            donorSnapshot?.LockoutEndUtc,
            model.Password,
            donorSnapshot?.PasswordHash,
            donorSnapshot?.PasswordSalt);
        var organizationValid = VerifyAccount(
            organizationSnapshot?.IsActive == true,
            organizationSnapshot?.LockoutEndUtc,
            model.Password,
            organizationSnapshot?.PasswordHash,
            organizationSnapshot?.PasswordSalt);

        // البريد يجب أن يكون فريدًا بين نوعي الحساب. وجود سجلين حالة حرجة حتى لو تطابقت كلمة مرور واحدة فقط.
        if (donorSnapshot != null && organizationSnapshot != null)
        {
            _logger.LogCritical(
                "Duplicate unified portal email detected for donor {DonorId} and organization {OrganizationId}",
                donorSnapshot.Id,
                organizationSnapshot.Id);
            await _audit.WriteAsync(
                HttpContext,
                "PortalDuplicateEmail",
                "The same normalized email is associated with donor and organization accounts.",
                success: false,
                severity: "Critical",
                cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                "تعذر تسجيل الدخول. تحقق من البريد الإلكتروني وكلمة المرور.");
            return View("~/Areas/Portal/Views/Auth/Login.cshtml", model);
        }

        if (donorValid && donorSnapshot != null)
        {
            var donor = await FinalizeDonorPasswordStepAtomicallyAsync(
                donorSnapshot.Id,
                model.Password,
                cancellationToken);

            if (donor != null)
            {
                return await SendOtpAndRedirectAsync(
                    "Donor",
                    donor.Id,
                    donor.Email ?? email,
                    donor.FullName,
                    model,
                    cancellationToken);
            }
        }
        else if (organizationValid && organizationSnapshot != null)
        {
            var organization = await FinalizeOrganizationPasswordStepAtomicallyAsync(
                organizationSnapshot.Id,
                model.Password,
                cancellationToken);

            if (organization != null)
            {
                return await SendOtpAndRedirectAsync(
                    "Organization",
                    organization.Id,
                    organization.Email ?? email,
                    organization.Name,
                    model,
                    cancellationToken);
            }
        }

        if (!donorValid && donorSnapshot is { IsActive: true })
            await RegisterDonorFailureAtomicallyAsync(donorSnapshot.Id, cancellationToken);

        if (!organizationValid && organizationSnapshot is { IsActive: true })
            await RegisterOrganizationFailureAtomicallyAsync(organizationSnapshot.Id, cancellationToken);

        await _audit.WriteAsync(
            HttpContext,
            "PortalLoginFailed",
            "Unified portal login credentials were rejected.",
            success: false,
            severity: "Warning",
            actorType: donorSnapshot != null ? "Donor" : organizationSnapshot != null ? "Organization" : null,
            actorId: donorSnapshot?.Id.ToString() ?? organizationSnapshot?.Id.ToString(),
            cancellationToken: cancellationToken);

        ModelState.AddModelError(
            string.Empty,
            "تعذر تسجيل الدخول. تحقق من البريد الإلكتروني وكلمة المرور.");

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
    [RequestSizeLimit(32 * 1024)]
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

        var verifiedChallenge = await _context.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ChallengeId == model.ChallengeId &&
                     x.PortalType == result.PortalType &&
                     x.AccountId == result.AccountId.Value,
                cancellationToken);

        if (verifiedChallenge == null)
        {
            ModelState.AddModelError(string.Empty, "طلب التحقق لم يعد صالحًا. سجل الدخول مرة أخرى.");
            return View("~/Areas/Portal/Views/Auth/VerifyOtp.cshtml", model);
        }

        if (string.Equals(result.PortalType, "Donor", StringComparison.OrdinalIgnoreCase))
        {
            var donor = await _context.DonorAccounts
                .FirstOrDefaultAsync(x => x.Id == result.AccountId.Value && x.IsActive, cancellationToken);
            if (donor == null ||
                LoginSecurity.IsLocked(donor.LockoutEndUtc) ||
                !string.Equals(donor.Email?.Trim(), verifiedChallenge.Email, StringComparison.OrdinalIgnoreCase) ||
                (donor.PasswordChangedAtUtc.HasValue &&
                 donor.PasswordChangedAtUtc.Value > verifiedChallenge.CreatedAtUtc))
            {
                ModelState.AddModelError(string.Empty, "طلب التحقق لم يعد صالحًا. سجل الدخول مرة أخرى.");
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
            if (organization == null ||
                LoginSecurity.IsLocked(organization.LockoutEndUtc) ||
                !string.Equals(organization.Email?.Trim(), verifiedChallenge.Email, StringComparison.OrdinalIgnoreCase) ||
                (organization.PasswordChangedAtUtc.HasValue &&
                 organization.PasswordChangedAtUtc.Value > verifiedChallenge.CreatedAtUtc))
            {
                ModelState.AddModelError(string.Empty, "طلب التحقق لم يعد صالحًا. سجل الدخول مرة أخرى.");
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
    [RequestSizeLimit(16 * 1024)]
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unable to resend portal OTP for challenge {ChallengeId}", challengeId);
            TempData["OtpError"] = "تعذر إعادة إرسال الرمز. حاول مرة أخرى.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { challengeId });
    }

    [HttpPost("/Portal/Logout")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = false
            });
    }

    private static bool VerifyAccount(
        bool isActive,
        DateTime? lockoutEndUtc,
        string password,
        string? hash,
        string? salt)
    {
        if (!isActive || LoginSecurity.IsLocked(lockoutEndUtc) ||
            string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            AdminPasswordHasher.VerifyDummy(password);
            return false;
        }

        return AdminPasswordHasher.VerifyPassword(password, hash, salt);
    }

    private async Task RegisterDonorFailureAtomicallyAsync(
        int donorId,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.DonorAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == donorId, cancellationToken);

            if (snapshot is not { IsActive: true } || LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
                return;

            var count = snapshot.AccessFailedCount;
            var lockout = snapshot.LockoutEndUtc;
            LoginSecurity.RegisterFailure(ref count, ref lockout, _securityOptions);

            var affected = await _context.DonorAccounts
                .Where(x =>
                    x.Id == donorId &&
                    x.IsActive &&
                    x.AccessFailedCount == snapshot.AccessFailedCount &&
                    x.LockoutEndUtc == snapshot.LockoutEndUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.AccessFailedCount, count)
                        .SetProperty(x => x.LockoutEndUtc, lockout)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now),
                    cancellationToken);

            if (affected == 1)
                return;
        }

        _logger.LogWarning(
            "Donor login failure counter had repeated concurrency conflicts for account {DonorId}",
            donorId);
    }

    private async Task RegisterOrganizationFailureAtomicallyAsync(
        int organizationId,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.OrganizationAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (snapshot is not { IsActive: true } || LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
                return;

            var count = snapshot.AccessFailedCount;
            var lockout = snapshot.LockoutEndUtc;
            LoginSecurity.RegisterFailure(ref count, ref lockout, _securityOptions);

            var affected = await _context.OrganizationAccounts
                .Where(x =>
                    x.Id == organizationId &&
                    x.IsActive &&
                    x.AccessFailedCount == snapshot.AccessFailedCount &&
                    x.LockoutEndUtc == snapshot.LockoutEndUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.AccessFailedCount, count)
                        .SetProperty(x => x.LockoutEndUtc, lockout)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now),
                    cancellationToken);

            if (affected == 1)
                return;
        }

        _logger.LogWarning(
            "Organization login failure counter had repeated concurrency conflicts for account {OrganizationId}",
            organizationId);
    }

    private async Task<DonorAccount?> FinalizeDonorPasswordStepAtomicallyAsync(
        int donorId,
        string password,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.DonorAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == donorId, cancellationToken);

            if (snapshot is not { IsActive: true } || LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
            {
                AdminPasswordHasher.VerifyDummy(password);
                return null;
            }

            if (!AdminPasswordHasher.VerifyPassword(
                    password,
                    snapshot.PasswordHash,
                    snapshot.PasswordSalt,
                    out var needsRehash))
            {
                await RegisterDonorFailureAtomicallyAsync(donorId, cancellationToken);
                return null;
            }

            var securityStamp = string.IsNullOrWhiteSpace(snapshot.SecurityStamp)
                ? LoginSecurity.NewSecurityStamp()
                : snapshot.SecurityStamp;
            var passwordHash = snapshot.PasswordHash;
            var passwordSalt = snapshot.PasswordSalt;
            var passwordChangedAtUtc = snapshot.PasswordChangedAtUtc;

            if (needsRehash)
            {
                var upgraded = AdminPasswordHasher.HashPassword(password);
                passwordHash = upgraded.Hash;
                passwordSalt = upgraded.Salt;
                passwordChangedAtUtc ??= DateTime.UtcNow;
            }

            var affected = await _context.DonorAccounts
                .Where(x =>
                    x.Id == donorId &&
                    x.IsActive &&
                    x.AccessFailedCount == snapshot.AccessFailedCount &&
                    x.LockoutEndUtc == snapshot.LockoutEndUtc &&
                    x.PasswordHash == snapshot.PasswordHash &&
                    x.PasswordSalt == snapshot.PasswordSalt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.AccessFailedCount, 0)
                        .SetProperty(x => x.LockoutEndUtc, (DateTime?)null)
                        .SetProperty(x => x.SecurityStamp, securityStamp)
                        .SetProperty(x => x.PasswordHash, passwordHash)
                        .SetProperty(x => x.PasswordSalt, passwordSalt)
                        .SetProperty(x => x.PasswordChangedAtUtc, passwordChangedAtUtc)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now),
                    cancellationToken);

            if (affected != 1)
                continue;

            snapshot.AccessFailedCount = 0;
            snapshot.LockoutEndUtc = null;
            snapshot.SecurityStamp = securityStamp;
            snapshot.PasswordHash = passwordHash;
            snapshot.PasswordSalt = passwordSalt;
            snapshot.PasswordChangedAtUtc = passwordChangedAtUtc;
            snapshot.UpdatedAt = DateTime.Now;
            return snapshot;
        }

        _logger.LogWarning(
            "Donor password step had repeated concurrency conflicts for account {DonorId}",
            donorId);
        return null;
    }

    private async Task<OrganizationAccount?> FinalizeOrganizationPasswordStepAtomicallyAsync(
        int organizationId,
        string password,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.OrganizationAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (snapshot is not { IsActive: true } || LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
            {
                AdminPasswordHasher.VerifyDummy(password);
                return null;
            }

            if (!AdminPasswordHasher.VerifyPassword(
                    password,
                    snapshot.PasswordHash,
                    snapshot.PasswordSalt,
                    out var needsRehash))
            {
                await RegisterOrganizationFailureAtomicallyAsync(organizationId, cancellationToken);
                return null;
            }

            var securityStamp = string.IsNullOrWhiteSpace(snapshot.SecurityStamp)
                ? LoginSecurity.NewSecurityStamp()
                : snapshot.SecurityStamp;
            var passwordHash = snapshot.PasswordHash;
            var passwordSalt = snapshot.PasswordSalt;
            var passwordChangedAtUtc = snapshot.PasswordChangedAtUtc;

            if (needsRehash)
            {
                var upgraded = AdminPasswordHasher.HashPassword(password);
                passwordHash = upgraded.Hash;
                passwordSalt = upgraded.Salt;
                passwordChangedAtUtc ??= DateTime.UtcNow;
            }

            var affected = await _context.OrganizationAccounts
                .Where(x =>
                    x.Id == organizationId &&
                    x.IsActive &&
                    x.AccessFailedCount == snapshot.AccessFailedCount &&
                    x.LockoutEndUtc == snapshot.LockoutEndUtc &&
                    x.PasswordHash == snapshot.PasswordHash &&
                    x.PasswordSalt == snapshot.PasswordSalt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.AccessFailedCount, 0)
                        .SetProperty(x => x.LockoutEndUtc, (DateTime?)null)
                        .SetProperty(x => x.SecurityStamp, securityStamp)
                        .SetProperty(x => x.PasswordHash, passwordHash)
                        .SetProperty(x => x.PasswordSalt, passwordSalt)
                        .SetProperty(x => x.PasswordChangedAtUtc, passwordChangedAtUtc)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now),
                    cancellationToken);

            if (affected != 1)
                continue;

            snapshot.AccessFailedCount = 0;
            snapshot.LockoutEndUtc = null;
            snapshot.SecurityStamp = securityStamp;
            snapshot.PasswordHash = passwordHash;
            snapshot.PasswordSalt = passwordSalt;
            snapshot.PasswordChangedAtUtc = passwordChangedAtUtc;
            snapshot.UpdatedAt = DateTime.Now;
            return snapshot;
        }

        _logger.LogWarning(
            "Organization password step had repeated concurrency conflicts for account {OrganizationId}",
            organizationId);
        return null;
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
