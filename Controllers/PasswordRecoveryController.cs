using Kafo.Web.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using Kafo.Web.Data;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PasswordRecoveryController : Controller
{
    private const string AdminPortal = "Admin";
    private const string UnifiedPortal = "Portal";

    private readonly ApplicationDbContext _db;
    private readonly ILoginOtpService _otpService;
    private readonly IPasswordSetupService _passwordSetup;
    private readonly ISecurityAuditService _audit;
    private readonly ILogger<PasswordRecoveryController> _logger;

    public PasswordRecoveryController(
        ApplicationDbContext db,
        ILoginOtpService otpService,
        IPasswordSetupService passwordSetup,
        ISecurityAuditService audit,
        ILogger<PasswordRecoveryController> logger)
    {
        _db = db;
        _otpService = otpService;
        _passwordSetup = passwordSetup;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet("/Admin/ForgotPassword")]
    public IActionResult AdminForgotPassword()
        => ForgotPasswordView(AdminPortal);

    [HttpGet("/Portal/ForgotPassword")]
    public IActionResult PortalForgotPassword()
        => ForgotPasswordView(UnifiedPortal);

    [HttpGet("/Donor/ForgotPassword")]
    [HttpGet("/Organizations/ForgotPassword")]
    public IActionResult LegacyPortalForgotPassword()
        => Redirect("/Portal/ForgotPassword");

    [HttpPost("/Admin/ForgotPassword")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public Task<IActionResult> AdminForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
        => SendResetOtpAsync(AdminPortal, model, cancellationToken);

    [HttpPost("/Portal/ForgotPassword")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public Task<IActionResult> PortalForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
        => SendResetOtpAsync(UnifiedPortal, model, cancellationToken);

    [HttpGet("/Account/VerifyPasswordResetOtp")]
    public async Task<IActionResult> VerifyPasswordResetOtp(
        string challengeId,
        CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(challengeId, cancellationToken);
        if (challenge == null || !TryGetAccountType(challenge.PortalType, out _))
        {
            TempData["PasswordResetError"] = "انتهت صلاحية طلب الاستعادة. ابدأ الطلب من جديد.";
            return Redirect("/Portal/ForgotPassword");
        }

        return View("~/Views/AccountSecurity/VerifyPasswordResetOtp.cshtml", new PasswordResetOtpViewModel
        {
            ChallengeId = challenge.ChallengeId,
            PortalType = GetPortalName(challenge.PortalType),
            MaskedEmail = challenge.MaskedEmail,
            ExpiresAtUtc = challenge.ExpiresAtUtc
        });
    }

    [HttpPost("/Account/VerifyPasswordResetOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> VerifyPasswordResetOtp(
        PasswordResetOtpViewModel model,
        CancellationToken cancellationToken)
    {
        var challenge = await _otpService.GetChallengeInfoAsync(model.ChallengeId, cancellationToken);
        if (challenge != null)
        {
            model.PortalType = GetPortalName(challenge.PortalType);
            model.MaskedEmail = challenge.MaskedEmail;
            model.ExpiresAtUtc = challenge.ExpiresAtUtc;
        }

        if (!ModelState.IsValid)
            return View("~/Views/AccountSecurity/VerifyPasswordResetOtp.cshtml", model);

        var result = await _otpService.VerifyAsync(model.ChallengeId, model.Code, cancellationToken);
        if (result.Status != LoginOtpVerificationStatus.Success ||
            result.AccountId == null ||
            !TryGetAccountType(result.PortalType, out var accountType))
        {
            AddOtpError(result.Status);
            return View("~/Views/AccountSecurity/VerifyPasswordResetOtp.cshtml", model);
        }

        var account = await ResolveAccountByIdAsync(accountType, result.AccountId.Value, cancellationToken);
        var verifiedChallenge = await _db.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ChallengeId == model.ChallengeId &&
                     x.PortalType == result.PortalType &&
                     x.AccountId == result.AccountId.Value,
                cancellationToken);

        if (account == null ||
            verifiedChallenge == null ||
            !string.Equals(account.Email.Trim(), verifiedChallenge.Email, StringComparison.OrdinalIgnoreCase) ||
            (account.PasswordChangedAtUtc.HasValue &&
             account.PasswordChangedAtUtc.Value > verifiedChallenge.CreatedAtUtc))
        {
            ModelState.AddModelError(string.Empty, "طلب الاستعادة لم يعد صالحًا. ابدأ الطلب من جديد.");
            return View("~/Views/AccountSecurity/VerifyPasswordResetOtp.cshtml", model);
        }

        var token = await _passwordSetup.CreateTokenAsync(
            account.AccountType,
            account.AccountId,
            requestedByAdminUserId: null,
            httpContext: HttpContext,
            cancellationToken: cancellationToken);

        await _audit.WriteAsync(
            HttpContext,
            "PasswordResetEmailVerified",
            "Account email ownership was verified by OTP before password reset.",
            success: true,
            actorType: account.AccountType,
            actorId: account.AccountId.ToString(),
            cancellationToken: cancellationToken);

        return Redirect($"/Account/SetPassword?token={Uri.EscapeDataString(token)}");
    }

    [HttpPost("/Account/ResendPasswordResetOtp")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> ResendPasswordResetOtp(
        string challengeId,
        string? portalType,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var challenge = IsValidChallengeId(challengeId)
            ? await _db.LoginOtpChallenges
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ChallengeId == challengeId &&
                         x.UsedAtUtc == null &&
                         x.ExpiresAtUtc > DateTime.UtcNow,
                    cancellationToken)
            : null;

        if (challenge == null || !TryGetAccountType(challenge.PortalType, out _))
        {
            TempData["PasswordResetError"] = "انتهت صلاحية طلب الاستعادة. ابدأ الطلب من جديد.";
            return Redirect(string.Equals(portalType, AdminPortal, StringComparison.OrdinalIgnoreCase)
                ? "/Admin/ForgotPassword"
                : "/Portal/ForgotPassword");
        }

        if (challenge.AccountId > 0)
        {
            try
            {
                await _otpService.ResendAsync(challengeId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // لا نكشف ما إذا كان الحساب حقيقيًا أو سبب فشل الإرسال.
                _logger.LogWarning(ex, "Unable to resend password reset OTP for challenge {ChallengeId}", challengeId);
            }
        }
        else
        {
            await _db.LoginOtpChallenges
                .Where(x => x.ChallengeId == challengeId && x.AccountId == 0 && x.UsedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.LastSentAtUtc, DateTime.UtcNow)
                        .SetProperty(x => x.SendCount, x => x.SendCount + 1),
                    cancellationToken);
        }

        await EnsureMinimumDurationAsync(stopwatch, cancellationToken);

        TempData["PasswordResetMessage"] =
            "إذا كان البريد مسجلاً لدينا، فسيصل رمز تحقق جديد خلال دقائق.";
        return RedirectToAction(nameof(VerifyPasswordResetOtp), new { challengeId });
    }

    private IActionResult ForgotPasswordView(string portalType)
    {
        return View("~/Views/AccountSecurity/ForgotPassword.cshtml", new ForgotPasswordViewModel
        {
            PortalType = portalType
        });
    }

    private async Task<IActionResult> SendResetOtpAsync(
        string portalType,
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        model.PortalType = portalType;
        if (!ModelState.IsValid)
            return View("~/Views/AccountSecurity/ForgotPassword.cshtml", model);

        var stopwatch = Stopwatch.StartNew();
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length > 180)
        {
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني يتجاوز الحد المسموح.");
            return View("~/Views/AccountSecurity/ForgotPassword.cshtml", model);
        }

        LoginOtpChallengeInfo? challengeInfo = null;
        RecoveryAccount? account = null;

        try
        {
            account = await ResolveAccountByEmailAsync(portalType, normalizedEmail, cancellationToken);
            if (account != null)
            {
                challengeInfo = await _otpService.CreateChallengeAsync(
                    new LoginOtpRequest(
                        $"{account.AccountType}PasswordReset",
                        account.AccountId,
                        account.Email,
                        account.DisplayName,
                        RememberMe: false,
                        ReturnUrl: null,
                        IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()),
                    cancellationToken);

                await _audit.WriteAsync(
                    HttpContext,
                    "PasswordResetOtpSent",
                    "Password reset OTP challenge was created.",
                    success: true,
                    actorType: account.AccountType,
                    actorId: account.AccountId.ToString(),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // نفس الاستجابة تُعرض للحساب الموجود وغير الموجود ولحالات الحد أو SMTP.
            _logger.LogWarning(
                ex,
                "Password reset OTP could not be created for portal {PortalType}",
                portalType);
        }

        if (challengeInfo == null)
        {
            challengeInfo = await CreateDecoyChallengeAsync(
                portalType,
                normalizedEmail,
                cancellationToken);

            await _audit.WriteAsync(
                HttpContext,
                "PasswordResetGenericRequest",
                "A generic password reset response was issued without confirming account existence.",
                success: account != null,
                severity: account == null ? "Warning" : "Information",
                actorType: account?.AccountType,
                actorId: account?.AccountId.ToString(),
                cancellationToken: cancellationToken);
        }

        await EnsureMinimumDurationAsync(stopwatch, cancellationToken);

        TempData["PasswordResetMessage"] =
            "إذا كان البريد مسجلاً لدينا، فسيصل رمز التحقق خلال دقائق.";

        return View("~/Views/AccountSecurity/VerifyPasswordResetOtp.cshtml", new PasswordResetOtpViewModel
        {
            ChallengeId = challengeInfo.ChallengeId,
            PortalType = portalType,
            MaskedEmail = MaskEmail(normalizedEmail),
            ExpiresAtUtc = challengeInfo.ExpiresAtUtc
        });
    }

    private async Task<LoginOtpChallengeInfo> CreateDecoyChallengeAsync(
        string portalType,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var resetType = string.Equals(portalType, AdminPortal, StringComparison.OrdinalIgnoreCase)
            ? "AdminPasswordReset"
            : "DonorPasswordReset";

        await _db.LoginOtpChallenges
            .Where(x => x.AccountId == 0 && x.ExpiresAtUtc < now.AddHours(-1))
            .ExecuteDeleteAsync(cancellationToken);

        var challenge = new LoginOtpChallenge
        {
            ChallengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            PortalType = resetType,
            AccountId = 0,
            Email = normalizedEmail,
            DisplayName = "المستخدم",
            RememberMe = false,
            ReturnUrl = null,
            IpAddress = NormalizeIp(HttpContext.Connection.RemoteIpAddress?.ToString()),
            // قيمة عشوائية ليست ناتج IDataProtector؛ خدمة OTP ستعاملها كتحدٍ غير صالح.
            ProtectedCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Attempts = 0,
            SendCount = 1,
            CreatedAtUtc = now,
            LastSentAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(10)
        };

        _db.LoginOtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        return new LoginOtpChallengeInfo(
            challenge.ChallengeId,
            challenge.PortalType,
            MaskEmail(challenge.Email),
            new DateTimeOffset(DateTime.SpecifyKind(challenge.ExpiresAtUtc, DateTimeKind.Utc)));
    }

    private async Task<RecoveryAccount?> ResolveAccountByEmailAsync(
        string portalType,
        string email,
        CancellationToken cancellationToken)
    {
        if (string.Equals(portalType, AdminPortal, StringComparison.OrdinalIgnoreCase))
        {
            return await _db.AdminUsers
                .AsNoTracking()
                .Where(x => x.IsActive && x.Email != null && x.Email.ToLower() == email)
                .Select(x => new RecoveryAccount("Admin", x.Id, x.Email!, x.FullName, x.PasswordChangedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var donor = await _db.DonorAccounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.Email != null && x.Email.ToLower() == email)
            .Select(x => new RecoveryAccount("Donor", x.Id, x.Email!, x.FullName, x.PasswordChangedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        var organization = await _db.OrganizationAccounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.Email != null && x.Email.ToLower() == email)
            .Select(x => new RecoveryAccount("Organization", x.Id, x.Email!, x.Name, x.PasswordChangedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (donor != null && organization != null)
        {
            _logger.LogWarning(
                "Password recovery rejected because email is shared by donor {DonorId} and organization {OrganizationId}",
                donor.AccountId,
                organization.AccountId);
            return null;
        }

        return donor ?? organization;
    }

    private async Task<RecoveryAccount?> ResolveAccountByIdAsync(
        string accountType,
        int accountId,
        CancellationToken cancellationToken)
    {
        if (accountType == "Admin")
        {
            return await _db.AdminUsers
                .AsNoTracking()
                .Where(x => x.Id == accountId && x.IsActive && x.Email != null)
                .Select(x => new RecoveryAccount("Admin", x.Id, x.Email!, x.FullName, x.PasswordChangedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (accountType == "Donor")
        {
            return await _db.DonorAccounts
                .AsNoTracking()
                .Where(x => x.Id == accountId && x.IsActive && x.Email != null)
                .Select(x => new RecoveryAccount("Donor", x.Id, x.Email!, x.FullName, x.PasswordChangedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (accountType == "Organization")
        {
            return await _db.OrganizationAccounts
                .AsNoTracking()
                .Where(x => x.Id == accountId && x.IsActive && x.Email != null)
                .Select(x => new RecoveryAccount("Organization", x.Id, x.Email!, x.Name, x.PasswordChangedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private static bool TryGetAccountType(string? resetPortalType, out string accountType)
    {
        accountType = resetPortalType?.Trim() switch
        {
            "AdminPasswordReset" => "Admin",
            "DonorPasswordReset" => "Donor",
            "OrganizationPasswordReset" => "Organization",
            _ => string.Empty
        };

        return accountType.Length > 0;
    }

    private static string GetPortalName(string resetPortalType)
        => string.Equals(resetPortalType, "AdminPasswordReset", StringComparison.Ordinal)
            ? AdminPortal
            : UnifiedPortal;

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

    private static bool IsValidChallengeId(string? challengeId)
        => !string.IsNullOrWhiteSpace(challengeId) &&
           challengeId.Length == 64 &&
           challengeId.All(Uri.IsHexDigit);

    private static string? NormalizeIp(string? ipAddress)
    {
        var value = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        return value is { Length: > 64 } ? value[..64] : value;
    }


    private static async Task EnsureMinimumDurationAsync(
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var minimumDuration = TimeSpan.FromMilliseconds(700);
        var remaining = minimumDuration - stopwatch.Elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, cancellationToken);
    }

    private void AddOtpError(LoginOtpVerificationStatus status)
    {
        var message = status switch
        {
            LoginOtpVerificationStatus.InvalidCode => "رمز التحقق غير صحيح.",
            LoginOtpVerificationStatus.Expired => "انتهت صلاحية رمز التحقق. ابدأ طلب الاستعادة من جديد.",
            LoginOtpVerificationStatus.Locked => "تم تجاوز عدد المحاولات المسموح. ابدأ طلب الاستعادة من جديد.",
            _ => "طلب الاستعادة غير موجود أو انتهت صلاحيته."
        };

        ModelState.AddModelError(string.Empty, message);
    }

    private sealed record RecoveryAccount(
        string AccountType,
        int AccountId,
        string Email,
        string DisplayName,
        DateTime? PasswordChangedAtUtc);
}
