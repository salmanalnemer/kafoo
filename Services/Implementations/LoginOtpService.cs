using System.Data;
using System.Security.Cryptography;
using System.Text;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Services.Implementations;

public sealed class LoginOtpService : ILoginOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MinimumSendInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(10);
    private const int MaxSendsPerWindow = 3;
    private const int MaxVerificationAttempts = 5;
    private const int MaxOptimisticRetries = 3;

    // قفل محلي محدود الذاكرة يمنع سباق إنشاء/إعادة إرسال الرموز داخل نسخة التطبيق.
    // المعاملة المتسلسلة تبقى طبقة الحماية الأساسية عند تشغيل أكثر من نسخة.
    private static readonly SemaphoreSlim SendGate = new(1, 1);

    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LoginOtpService> _logger;

    public LoginOtpService(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        ILogger<LoginOtpService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _protector = dataProtectionProvider.CreateProtector("Kafo.Web.LoginOtp.v1");
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LoginOtpChallengeInfo> CreateChallengeAsync(
        LoginOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail.Length is 0 or > 180)
            throw new InvalidOperationException("عنوان البريد الإلكتروني غير صالح.");

        var normalizedPortalType = NormalizePortalType(request.PortalType);
        var normalizedIp = NormalizeIp(request.IpAddress);

        await SendGate.WaitAsync(cancellationToken);
        LoginOtpChallenge? challenge = null;

        try
        {
            var now = UtcNow();
            var code = GenerateCode();

            await using (var transaction = await _db.Database.BeginTransactionAsync(
                             IsolationLevel.Serializable,
                             cancellationToken))
            {
                await EnsureCanSendAsync(normalizedEmail, normalizedIp, now, cancellationToken);

                await _db.LoginOtpChallenges
                    .Where(x =>
                        x.PortalType == normalizedPortalType &&
                        x.AccountId == request.AccountId &&
                        x.UsedAtUtc == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.UsedAtUtc, now),
                        cancellationToken);

                challenge = new LoginOtpChallenge
                {
                    ChallengeId = NewChallengeId(),
                    PortalType = normalizedPortalType,
                    AccountId = request.AccountId,
                    Email = normalizedEmail,
                    DisplayName = NormalizeDisplayName(request.DisplayName),
                    RememberMe = request.RememberMe,
                    ReturnUrl = IsSafeReturnUrl(request.ReturnUrl) ? request.ReturnUrl : null,
                    IpAddress = normalizedIp,
                    ProtectedCode = _protector.Protect(code),
                    Attempts = 0,
                    SendCount = 1,
                    CreatedAtUtc = now,
                    LastSentAtUtc = now,
                    ExpiresAtUtc = now.Add(OtpLifetime)
                };

                _db.LoginOtpChallenges.Add(challenge);
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            try
            {
                await SendOtpEmailAsync(
                    challenge.PortalType,
                    challenge.Email,
                    challenge.DisplayName,
                    code,
                    cancellationToken);
            }
            catch
            {
                // لا نترك تحديًا فعالًا إذا فشل إرسال البريد.
                await _db.LoginOtpChallenges
                    .Where(x => x.ChallengeId == challenge.ChallengeId)
                    .ExecuteDeleteAsync(CancellationToken.None);
                throw;
            }

            return ToInfo(challenge);
        }
        finally
        {
            SendGate.Release();
        }
    }

    public async Task<LoginOtpChallengeInfo?> GetChallengeInfoAsync(
        string challengeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidChallengeId(challengeId))
            return null;

        var now = UtcNow();
        var state = await _db.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ChallengeId == challengeId &&
                     x.UsedAtUtc == null &&
                     x.ExpiresAtUtc > now,
                cancellationToken);

        return state == null ? null : ToInfo(state);
    }

    public async Task<LoginOtpVerificationResult> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidChallengeId(challengeId) || string.IsNullOrWhiteSpace(code))
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);

        var candidateCode = code.Trim();
        if (candidateCode.Length != 6 || candidateCode.Any(static c => c is < '0' or > '9'))
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.InvalidCode);

        // تحديث مشروط على Attempts وUsedAtUtc يمنع نجاح طلبين متزامنين لنفس الرمز.
        for (var retry = 0; retry < MaxOptimisticRetries; retry++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var state = await _db.LoginOtpChallenges
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);

            if (state == null || state.UsedAtUtc != null)
                return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);

            var now = UtcNow();
            if (state.ExpiresAtUtc <= now)
            {
                var expiredUpdated = await MarkUsedConditionallyAsync(state, now, cancellationToken);
                if (expiredUpdated)
                    return new LoginOtpVerificationResult(LoginOtpVerificationStatus.Expired);

                continue;
            }

            if (state.Attempts >= MaxVerificationAttempts)
            {
                var lockedUpdated = await MarkUsedConditionallyAsync(state, now, cancellationToken);
                if (lockedUpdated)
                    return new LoginOtpVerificationResult(LoginOtpVerificationStatus.Locked);

                continue;
            }

            string expectedCode;
            try
            {
                expectedCode = _protector.Unprotect(state.ProtectedCode);
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Unable to unprotect OTP challenge {ChallengeId}", challengeId);
                var invalidUpdated = await MarkUsedConditionallyAsync(state, now, cancellationToken);
                if (invalidUpdated)
                    return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);

                continue;
            }

            var valid = FixedTimeEquals(expectedCode, candidateCode);
            var nextAttempts = state.Attempts + 1;
            var mustLock = !valid && nextAttempts >= MaxVerificationAttempts;
            var usedAt = valid || mustLock ? now : (DateTime?)null;

            var affected = await _db.LoginOtpChallenges
                .Where(x =>
                    x.ChallengeId == state.ChallengeId &&
                    x.UsedAtUtc == null &&
                    x.Attempts == state.Attempts &&
                    x.ExpiresAtUtc == state.ExpiresAtUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Attempts, nextAttempts)
                        .SetProperty(x => x.UsedAtUtc, usedAt),
                    cancellationToken);

            if (affected != 1)
                continue;

            if (!valid)
            {
                return new LoginOtpVerificationResult(
                    mustLock
                        ? LoginOtpVerificationStatus.Locked
                        : LoginOtpVerificationStatus.InvalidCode);
            }

            return new LoginOtpVerificationResult(
                LoginOtpVerificationStatus.Success,
                state.PortalType,
                state.AccountId,
                state.RememberMe,
                state.ReturnUrl);
        }

        // حصل تعارض متكرر؛ نرجع نتيجة عامة ولا نخاطر بقبول الرمز مرتين.
        return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);
    }

    public async Task<LoginOtpChallengeInfo> ResendAsync(
        string challengeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidChallengeId(challengeId))
            throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

        var snapshot = await _db.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);

        if (snapshot == null)
            throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

        await SendGate.WaitAsync(cancellationToken);

        try
        {
            LoginOtpChallenge state;
            string code;

            await using (var transaction = await _db.Database.BeginTransactionAsync(
                             IsolationLevel.Serializable,
                             cancellationToken))
            {
                state = await _db.LoginOtpChallenges
                    .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken)
                    ?? throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

                var now = UtcNow();
                if (state.UsedAtUtc != null || state.ExpiresAtUtc <= now)
                    throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

                await EnsureCanSendAsync(state.Email, state.IpAddress, now, cancellationToken);

                code = GenerateCode();
                state.ProtectedCode = _protector.Protect(code);
                state.Attempts = 0;
                state.SendCount++;
                state.LastSentAtUtc = now;
                state.ExpiresAtUtc = now.Add(OtpLifetime);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            try
            {
                await SendOtpEmailAsync(
                    state.PortalType,
                    state.Email,
                    state.DisplayName,
                    code,
                    cancellationToken);
            }
            catch
            {
                // الرمز الجديد لم يصل؛ إبطال التحدي يمنع بقاء رمز مجهول فعال.
                var now = UtcNow();
                await _db.LoginOtpChallenges
                    .Where(x => x.ChallengeId == challengeId && x.UsedAtUtc == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.UsedAtUtc, now),
                        CancellationToken.None);
                throw;
            }

            return ToInfo(state);
        }
        finally
        {
            SendGate.Release();
        }
    }

    private async Task<bool> MarkUsedConditionallyAsync(
        LoginOtpChallenge state,
        DateTime usedAtUtc,
        CancellationToken cancellationToken)
    {
        var affected = await _db.LoginOtpChallenges
            .Where(x =>
                x.ChallengeId == state.ChallengeId &&
                x.UsedAtUtc == null &&
                x.Attempts == state.Attempts &&
                x.ExpiresAtUtc == state.ExpiresAtUtc)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UsedAtUtc, usedAtUtc),
                cancellationToken);

        return affected == 1;
    }

    private Task SendOtpEmailAsync(
        string portalType,
        string email,
        string displayName,
        string code,
        CancellationToken cancellationToken)
    {
        return portalType.EndsWith("PasswordReset", StringComparison.OrdinalIgnoreCase)
            ? _emailSender.SendPasswordResetOtpAsync(
                email, displayName, code, OtpLifetime, cancellationToken)
            : _emailSender.SendLoginOtpAsync(
                email, displayName, code, OtpLifetime, cancellationToken);
    }

    private async Task EnsureCanSendAsync(
        string email,
        string? ipAddress,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var windowStart = now.Subtract(RateWindow);
        var recent = await _db.LoginOtpChallenges
            .AsNoTracking()
            .Where(x =>
                x.LastSentAtUtc >= windowStart &&
                (x.Email == email ||
                 (!string.IsNullOrEmpty(ipAddress) && x.IpAddress == ipAddress)))
            .Select(x => new { x.LastSentAtUtc, x.SendCount })
            .ToListAsync(cancellationToken);

        var lastSent = recent
            .OrderByDescending(x => x.LastSentAtUtc)
            .FirstOrDefault()
            ?.LastSentAtUtc;

        if (lastSent.HasValue)
        {
            var wait = MinimumSendInterval - (now - lastSent.Value);
            if (wait > TimeSpan.Zero)
            {
                throw new OtpRateLimitException(
                    $"يمكن إعادة إرسال الرمز بعد {Math.Ceiling(wait.TotalSeconds):0} ثانية.");
            }
        }

        if (recent.Sum(x => x.SendCount) >= MaxSendsPerWindow)
            throw new OtpRateLimitException("تم تجاوز الحد المسموح لإرسال رموز التحقق. حاول بعد 10 دقائق.");
    }

    private static bool FixedTimeEquals(string expected, string candidate)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var candidateBytes = Encoding.UTF8.GetBytes(candidate);

        // كلا الرمزين متوقع أن يكونا 6 أرقام؛ عند اختلاف الطول لا يتم القبول.
        return expectedBytes.Length == candidateBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, candidateBytes);
    }

    private static LoginOtpChallengeInfo ToInfo(LoginOtpChallenge state)
        => new(
            state.ChallengeId,
            state.PortalType,
            MaskEmail(state.Email),
            new DateTimeOffset(DateTime.SpecifyKind(state.ExpiresAtUtc, DateTimeKind.Utc)));

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static string GenerateCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string NewChallengeId()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizePortalType(string portalType)
    {
        var value = (portalType ?? string.Empty).Trim();
        if (value.Length is 0 or > 30 ||
            value.Any(static c => !char.IsLetterOrDigit(c)))
        {
            throw new InvalidOperationException("نوع بوابة التحقق غير صالح.");
        }

        return value;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        var value = string.IsNullOrWhiteSpace(displayName) ? "المستخدم" : displayName.Trim();
        return value.Length <= 220 ? value : value[..220];
    }

    private static string? NormalizeIp(string? ipAddress)
    {
        var value = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        return value is { Length: > 64 } ? value[..64] : value;
    }

    private static bool IsValidChallengeId(string? challengeId)
        => !string.IsNullOrWhiteSpace(challengeId) &&
           challengeId.Length == 64 &&
           challengeId.All(Uri.IsHexDigit);

    private static bool IsSafeReturnUrl(string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl) ||
           (returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.Contains('\\'));

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
