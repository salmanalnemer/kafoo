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
    public async Task<IActionResult> Login(AdminLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email, cancellationToken);

        var locked = user != null && LoginSecurity.IsLocked(user.LockoutEndUtc);
        var credentialsValid = false;
        var needsRehash = false;

        if (user != null && user.IsActive && !locked)
        {
            credentialsValid = AdminPasswordHasher.VerifyPassword(
                model.Password,
                user.PasswordHash,
                user.PasswordSalt,
                out needsRehash);
        }
        else
        {
            AdminPasswordHasher.VerifyDummy(model.Password);
        }

        if (!credentialsValid || user == null)
        {
            var retryAfter = LoginSecurity.GetRemainingLockout(user?.LockoutEndUtc);

            if (user is { IsActive: true } && !locked)
            {
                var failedCount = user.AccessFailedCount;
                var lockoutEnd = user.LockoutEndUtc;
                var newLockout = LoginSecurity.RegisterFailure(
                    ref failedCount,
                    ref lockoutEnd,
                    _securityOptions);

                user.AccessFailedCount = failedCount;
                user.LockoutEndUtc = lockoutEnd;
                user.UpdatedAt = DateTime.Now;
                retryAfter = MaxDuration(retryAfter, newLockout);

                await _context.SaveChangesAsync(cancellationToken);
            }

            await _audit.WriteAsync(
                HttpContext,
                "AdminLoginFailed",
                "Admin login credentials were rejected.",
                success: false,
                severity: "Warning",
                actorType: "Admin",
                actorId: user?.Id.ToString(),
                cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                BuildLoginFailureMessage(retryAfter));

            return View("~/Areas/Admin/Views/Auth/Login.cshtml", model);
        }

        var accessFailedCount = user.AccessFailedCount;
        var currentLockoutEnd = user.LockoutEndUtc;
        LoginSecurity.Reset(ref accessFailedCount, ref currentLockoutEnd);
        user.AccessFailedCount = accessFailedCount;
        user.LockoutEndUtc = currentLockoutEnd;

        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            user.SecurityStamp = LoginSecurity.NewSecurityStamp();

        if (needsRehash)
        {
            var upgraded = AdminPasswordHasher.HashPassword(model.Password);
            user.PasswordHash = upgraded.Hash;
            user.PasswordSalt = upgraded.Salt;
            user.PasswordChangedAtUtc ??= DateTime.UtcNow;
        }

        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var challenge = await _otpService.CreateChallengeAsync(
                new LoginOtpRequest(
                    "Admin",
                    user.Id,
                    email,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send admin login OTP for account {AdminUserId}", user.Id);
            ModelState.AddModelError(string.Empty,
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

        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == result.AccountId.Value && x.IsActive, cancellationToken);

        if (user == null || LoginSecurity.IsLocked(user.LockoutEndUtc))
        {
            ModelState.AddModelError(string.Empty, "الحساب غير موجود أو غير مفعل.");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to resend admin OTP for challenge {ChallengeId}", challengeId);
            TempData["OtpError"] = "تعذر إعادة إرسال الرمز. حاول مرة أخرى.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { challengeId });
    }

    [HttpPost("/Admin/Logout")]
    [ValidateAntiForgeryToken]
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
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = false
            });
    }

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
