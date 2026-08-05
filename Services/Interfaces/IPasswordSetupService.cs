namespace Kafo.Web.Services.Interfaces;

public interface IPasswordSetupService
{
    Task<string> CreateTokenAsync(
        string accountType,
        int accountId,
        int? requestedByAdminUserId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task IssueAsync(
        string accountType,
        int accountId,
        string recipientEmail,
        string recipientName,
        string accountLabel,
        int? requestedByAdminUserId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
