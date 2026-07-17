namespace Kafo.Web.Services.Interfaces;

public sealed record LoginOtpRequest(
    string PortalType,
    int AccountId,
    string Email,
    string DisplayName,
    bool RememberMe,
    string? ReturnUrl,
    string? IpAddress);

public sealed record LoginOtpChallengeInfo(
    string ChallengeId,
    string PortalType,
    string MaskedEmail,
    DateTimeOffset ExpiresAtUtc);

public enum LoginOtpVerificationStatus
{
    Success,
    InvalidCode,
    Expired,
    Locked,
    NotFound
}

public sealed record LoginOtpVerificationResult(
    LoginOtpVerificationStatus Status,
    string? PortalType = null,
    int? AccountId = null,
    bool RememberMe = false,
    string? ReturnUrl = null);

public interface ILoginOtpService
{
    Task<LoginOtpChallengeInfo> CreateChallengeAsync(
        LoginOtpRequest request,
        CancellationToken cancellationToken = default);

    Task<LoginOtpChallengeInfo?> GetChallengeInfoAsync(
        string challengeId,
        CancellationToken cancellationToken = default);

    Task<LoginOtpVerificationResult> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken = default);

    Task<LoginOtpChallengeInfo> ResendAsync(
        string challengeId,
        CancellationToken cancellationToken = default);
}

public sealed class OtpRateLimitException : Exception
{
    public OtpRateLimitException(string message) : base(message) { }
}
