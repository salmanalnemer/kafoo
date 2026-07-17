namespace Kafo.Web.Services.Interfaces;

public interface IPasswordSetupService
{
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
