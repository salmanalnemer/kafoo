using System.Security.Claims;
using Kafo.Web.Configuration;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Kafo.Web.ViewModels.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminAuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILoginOtpService _otpService;
    private readonly ISecurityAuditService _audit;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        ApplicationDbContext context,
        ILoginOtpService otpService,
        ISecurityAuditService audit,
        IOptions<SecurityOptions> securityOptions,
        ILogger<AdminAuthController> logger)
    {
        _context = context;
        _otpService = otpService;
        _audit = audit;
        _securityOptions = securityOptions.Value;
        _logger = logger;
    }

    [HttpGet("/Admin/Login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var authResult = await HttpContext.AuthenticateAsync(KafoAuthSchemes.Admin);
        if (authResult.Succeeded)
            return Redirect("/Admin");

        return View("~/Areas/Admin/Views/Auth/Login.cshtml", new AdminLoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("/Admin/Login")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Login(AdminLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);

        var email = model.Email.Trim().ToLowerInvariant();
        var snapshot = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Email != null && x.Email.ToLower() == email,
                cancellationToken);

        var credentialsValid = false;
        if (snapshot is { IsActive: true } && !LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
        {
            credentialsValid = AdminPasswordHasher.VerifyPassword(
                model.Password,
                snapshot.PasswordHash,
                snapshot.PasswordSalt);
        }
        else
        {
            AdminPasswordHasher.VerifyDummy(model.Password);
        }

        if (!credentialsValid || snapshot == null)
        {
            if (snapshot is { IsActive: true })
                await RegisterFailureAtomicallyAsync(snapshot.Id, cancellationToken);

            await _audit.WriteAsync(
                HttpContext,
                "AdminLoginFailed",
                "Admin login credentials were rejected.",
                success: false,
                severity: "Warning",
                actorType: "Admin",
                actorId: snapshot?.Id.ToString(),
                cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                "تعذر تسجيل الدخول. تحقق من البريد الإلكتروني وكلمة المرور.");

            return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);
        }

        var user = await FinalizePasswordStepAtomicallyAsync(
            snapshot.Id,
            model.Password,
            cancellationToken);

        if (user == null)
        {
            await _audit.WriteAsync(
                HttpContext,
                "AdminLoginFailed",
                "Administrator account changed or was locked during authentication.",
                success: false,
                severity: "Warning",
                actorType: "Admin",
                actorId: snapshot.Id.ToString(),
                cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                "تعذر تسجيل الدخول. تحقق من البريد الإلكتروني وكلمة المرور.");
            return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);
        }

        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    "Admin",
                    user.Id,
                    user.Email ?? email,
                    user.FullName,
                    model.RememberMe,
                    model.ReturnUrl,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            await _audit.WriteAsync(
                HttpContext,
                "AdminOtpSent",
                "OTP challenge created for administrator login.",
                success: true,
                actorType: "Admin",
                actorId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            return RedirectToAction(nameof(VerifyOtp), new { challengeId = challenge.ChallengeId });
        }
        catch (OtpRateLimitException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unable to send admin login OTP for account {AdminUserId}", user.Id);
            ModelState.AddModelError(
                string.Empty,
                "تعذر إرسال رمز التحقق. تأكد من إعدادات البريد الإلكتروني ثم حاول مجددًا.");
        }

        return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);
    }

    [HttpGet("/Admin/VerifyOtp")]
    public async Task<IActionResult> VerifyOtp(string challengeId, CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(challengeId, cancellationToken);
        if (challenge == null ||
            !string.Equals(challenge.PortalType, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["LoginError"] = "انتهت صلاحية طلب التحقق. سجل الدخول مرة أخرى.";
            return Redirect("/Admin/Login");
        }

        return View("~/Areas/Admin/Views/Auth/VerifyOtp.cshtml", new OtpVerificationViewModel
        {
            ChallengeId = challenge.ChallengeId,
            MaskedEmail = challenge.MaskedEmail,
            ExpiresAtUtc = challenge.ExpiresAtUtc
        });
    }

    [HttpPost("/Admin/VerifyOtp")]
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
            return View("~/Areas/Admin/Views/Auth/VerifyOtp.cshtml", model);

        var result = await _otpService.VerifyAsync(model.ChallengeId, model.Code, cancellationToken);
        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId == null ||
            !string.Equals(result.PortalType, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await _audit.WriteAsync(
                HttpContext,
                "AdminOtpFailed",
                $"Administrator OTP verification failed with status {result.Status}.",
                success: false,
                severity: "Warning",
                actorType: "Admin",
                actorId: result.AccountId?.ToString(),
                cancellationToken: cancellationToken);

            AddOtpError(result.Status == LoginOtpVerificationStatus.Success
                ? LoginOtpVerificationStatus.NotFound
                : result.Status);
            return View("~/Areas/Admin/Views/Auth/VerifyOtp.cshtml", model);
        }

        var verifiedChallenge = await _context.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ChallengeId == model.ChallengeId &&
                     x.PortalType == "Admin" &&
                     x.AccountId == result.AccountId.Value,
                cancellationToken);

        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == result.AccountId.Value && x.IsActive, cancellationToken);

        if (user == null ||
            verifiedChallenge == null ||
            LoginSecurity.IsLocked(user.LockoutEndUtc) ||
            !string.Equals(user.Email?.Trim(), verifiedChallenge.Email, StringComparison.OrdinalIgnoreCase) ||
            (user.PasswordChangedAtUtc.HasValue &&
             user.PasswordChangedAtUtc.Value > verifiedChallenge.CreatedAtUtc))
        {
            ModelState.AddModelError(string.Empty, "طلب التحقق لم يعد صالحًا. سجل الدخول مرة أخرى.");
            return View("~/Areas/Admin/Views/Auth/VerifyOtp.cshtml", model);
        }

        user.LastLoginAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        await SignInAdminAsync(user, result.RememberMe);
        await _audit.WriteAsync(
            HttpContext,
            "AdminLoginSucceeded",
            "Administrator completed password and OTP authentication.",
            success: true,
            actorType: "Admin",
            actorId: user.Id.ToString(),
            cancellationToken: cancellationToken);

        if (user.MustChangePassword)
            return Redirect("/Admin/Profile#change-password");

        if (!string.IsNullOrWhiteSpace(result.ReturnUrl) &&
            Url.IsLocalUrl(result.ReturnUrl) &&
            result.ReturnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) &&
            await CanAccessPathAsync(user, result.ReturnUrl, cancellationToken))
        {
            return Redirect(result.ReturnUrl);
        }

        return Redirect(await GetDefaultLandingUrlAsync(user, cancellationToken));
    }

    [HttpPost("/Admin/ResendOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> ResendOtp(string challengeId, CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(challengeId, cancellationToken);
        if (challenge == null ||
            !string.Equals(challenge.PortalType, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["LoginError"] = "انتهت صلاحية طلب التحقق. سجل الدخول مرة أخرى.";
            return Redirect("/Admin/Login");
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
            _logger.LogError(ex, "Unable to resend admin OTP for challenge {ChallengeId}", challengeId);
            TempData["OtpError"] = "تعذر إعادة إرسال الرمز. حاول مرة أخرى.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { challengeId });
    }

    [HttpPost("/Admin/Logout")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue("KafoAdminUserId");
        await HttpContext.SignOutAsync(KafoAuthSchemes.Admin);
        await _audit.WriteAsync(
            HttpContext,
            "AdminLogout",
            "Administrator signed out.",
            success: true,
            actorType: "Admin",
            actorId: adminId,
            cancellationToken: cancellationToken);
        return Redirect("/Admin/Login");
    }

    private async Task<bool> CanAccessPathAsync(
        AdminUser user,
        string path,
        CancellationToken cancellationToken)
    {
        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (AdminRolePolicy.HasFullPageAccess(roleCode))
            return true;

        var normalized = AdminPagesCatalog.Normalize(path);
        if (normalized.StartsWith(AdminRolePolicy.ProfilePagePath, StringComparison.OrdinalIgnoreCase))
            return true;

        var page = AdminPagesCatalog.Match(normalized);
        if (page == null)
            return false;

        return await _context.AdminPagePermissions
            .AsNoTracking()
            .AnyAsync(x =>
                x.AdminUserId == user.Id &&
                x.PagePath == page.PagePath &&
                x.CanAccess,
                cancellationToken);
    }

    private async Task<string> GetDefaultLandingUrlAsync(
        AdminUser user,
        CancellationToken cancellationToken)
    {
        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (AdminRolePolicy.HasFullPageAccess(roleCode))
            return AdminRolePolicy.DashboardPagePath;

        var allowedPaths = (await _context.AdminPagePermissions
                .AsNoTracking()
                .Where(x => x.AdminUserId == user.Id && x.CanAccess)
                .Select(x => x.PagePath)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return AdminPagesCatalog.Pages
                   .FirstOrDefault(x => allowedPaths.Contains(x.PagePath))
                   ?.PagePath
               ?? AdminRolePolicy.ProfilePagePath;
    }

    private async Task SignInAdminAsync(AdminUser user, bool rememberMe)
    {
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

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, KafoAuthSchemes.Admin));
        await HttpContext.SignInAsync(
            KafoAuthSchemes.Admin,
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

    private async Task RegisterFailureAtomicallyAsync(
        int adminUserId,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);

            if (snapshot is not { IsActive: true } || LoginSecurity.IsLocked(snapshot.LockoutEndUtc))
                return;

            var failedCount = snapshot.AccessFailedCount;
            var lockoutEnd = snapshot.LockoutEndUtc;
            LoginSecurity.RegisterFailure(
                ref failedCount,
                ref lockoutEnd,
                _securityOptions);

            var affected = await _context.AdminUsers
                .Where(x =>
                    x.Id == adminUserId &&
                    x.IsActive &&
                    x.AccessFailedCount == snapshot.AccessFailedCount &&
                    x.LockoutEndUtc == snapshot.LockoutEndUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.AccessFailedCount, failedCount)
                        .SetProperty(x => x.LockoutEndUtc, lockoutEnd)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now),
                    cancellationToken);

            if (affected == 1)
                return;
        }

        _logger.LogWarning(
            "Admin login failure counter had repeated concurrency conflicts for account {AdminUserId}",
            adminUserId);
    }

    private async Task<AdminUser?> FinalizePasswordStepAtomicallyAsync(
        int adminUserId,
        string password,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var snapshot = await _context.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);

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
                await RegisterFailureAtomicallyAsync(adminUserId, cancellationToken);
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

            var affected = await _context.AdminUsers
                .Where(x =>
                    x.Id == adminUserId &&
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
            "Admin password step had repeated concurrency conflicts for account {AdminUserId}",
            adminUserId);
        return null;
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
