using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public LoginOtpService(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        _db = db;
        _emailSender = emailSender;
        _protector = dataProtectionProvider.CreateProtector("Kafo.Web.LoginOtp.v1");
        _timeProvider = timeProvider;
    }

    public async Task<LoginOtpChallengeInfo> CreateChallengeAsync(
        LoginOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var key = $"{normalizedEmail}|{request.IpAddress}";
        var gate = Gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureCanSendAsync(normalizedEmail, request.IpAddress, cancellationToken);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var previous = await _db.LoginOtpChallenges
                .Where(x =>
                    x.PortalType == request.PortalType &&
                    x.AccountId == request.AccountId &&
                    x.UsedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var item in previous)
                item.UsedAtUtc = now;

            var code = GenerateCode();
            var challenge = new LoginOtpChallenge
            {
                ChallengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                PortalType = request.PortalType,
                AccountId = request.AccountId,
                Email = normalizedEmail,
                DisplayName = request.DisplayName.Trim(),
                RememberMe = request.RememberMe,
                ReturnUrl = IsSafeReturnUrl(request.ReturnUrl) ? request.ReturnUrl : null,
                IpAddress = request.IpAddress,
                ProtectedCode = _protector.Protect(code),
                CreatedAtUtc = now,
                LastSentAtUtc = now,
                ExpiresAtUtc = now.Add(OtpLifetime),
                SendCount = 1
            };

            _db.LoginOtpChallenges.Add(challenge);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                await _emailSender.SendLoginOtpAsync(
                    normalizedEmail,
                    challenge.DisplayName,
                    code,
                    OtpLifetime,
                    cancellationToken);
            }
            catch
            {
                _db.LoginOtpChallenges.Remove(challenge);
                await _db.SaveChangesAsync(CancellationToken.None);
                throw;
            }

            return ToInfo(challenge);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LoginOtpChallengeInfo?> GetChallengeInfoAsync(
        string challengeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidChallengeId(challengeId))
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var state = await _db.LoginOtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ChallengeId == challengeId &&
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

        var state = await _db.LoginOtpChallenges
            .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (state == null || state.UsedAtUtc != null)
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (state.ExpiresAtUtc <= now)
        {
            state.UsedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.Expired);
        }

        if (state.Attempts >= MaxVerificationAttempts)
        {
            state.UsedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.Locked);
        }

        state.Attempts++;
        string expectedCode;
        try
        {
            expectedCode = _protector.Unprotect(state.ProtectedCode);
        }
        catch (CryptographicException)
        {
            state.UsedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return new LoginOtpVerificationResult(LoginOtpVerificationStatus.NotFound);
        }

        var valid = FixedTimeEquals(expectedCode, code.Trim());
        if (!valid)
        {
            if (state.Attempts >= MaxVerificationAttempts)
                state.UsedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return new LoginOtpVerificationResult(
                state.Attempts >= MaxVerificationAttempts
                    ? LoginOtpVerificationStatus.Locked
                    : LoginOtpVerificationStatus.InvalidCode);
        }

        state.UsedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return new LoginOtpVerificationResult(
            LoginOtpVerificationStatus.Success,
            state.PortalType,
            state.AccountId,
            state.RememberMe,
            state.ReturnUrl);
    }

    public async Task<LoginOtpChallengeInfo> ResendAsync(
        string challengeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidChallengeId(challengeId))
            throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

        var state = await _db.LoginOtpChallenges
            .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (state == null || state.UsedAtUtc != null || state.ExpiresAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("طلب التحقق غير موجود أو انتهت صلاحيته.");

        var key = $"{state.Email}|{state.IpAddress}";
        var gate = Gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureCanSendAsync(state.Email, state.IpAddress, cancellationToken);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var code = GenerateCode();

            await _emailSender.SendLoginOtpAsync(
                state.Email,
                state.DisplayName,
                code,
                OtpLifetime,
                cancellationToken);

            state.ProtectedCode = _protector.Protect(code);
            state.Attempts = 0;
            state.SendCount++;
            state.LastSentAtUtc = now;
            state.ExpiresAtUtc = now.Add(OtpLifetime);
            await _db.SaveChangesAsync(cancellationToken);
            return ToInfo(state);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EnsureCanSendAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.Subtract(RateWindow);
        var recent = await _db.LoginOtpChallenges
            .AsNoTracking()
            .Where(x =>
                x.LastSentAtUtc >= windowStart &&
                (x.Email == email || (!string.IsNullOrEmpty(ipAddress) && x.IpAddress == ipAddress)))
            .ToListAsync(cancellationToken);

        var lastSent = recent.OrderByDescending(x => x.LastSentAtUtc).FirstOrDefault()?.LastSentAtUtc;
        if (lastSent.HasValue)
        {
            var wait = MinimumSendInterval - (now - lastSent.Value);
            if (wait > TimeSpan.Zero)
                throw new OtpRateLimitException($"يمكن إعادة إرسال الرمز بعد {Math.Ceiling(wait.TotalSeconds):0} ثانية.");
        }

        if (recent.Sum(x => x.SendCount) >= MaxSendsPerWindow)
            throw new OtpRateLimitException("تم تجاوز الحد المسموح لإرسال رموز التحقق. حاول بعد 10 دقائق.");
    }

    private static bool FixedTimeEquals(string expected, string candidate)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var candidateBytes = Encoding.UTF8.GetBytes(candidate);
        return expectedBytes.Length == candidateBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, candidateBytes);
    }

    private static LoginOtpChallengeInfo ToInfo(LoginOtpChallenge state)
        => new(
            state.ChallengeId,
            state.PortalType,
            MaskEmail(state.Email),
            new DateTimeOffset(DateTime.SpecifyKind(state.ExpiresAtUtc, DateTimeKind.Utc)));

    private static string GenerateCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

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
        if (atIndex <= 0) return "***";
        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
