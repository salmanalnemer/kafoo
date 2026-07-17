namespace Kafo.Web.Services.Interfaces;

public interface ISecurityAuditService
{
    Task WriteAsync(
        HttpContext context,
        string eventType,
        string message,
        bool success,
        string severity = "Information",
        string? actorType = null,
        string? actorId = null,
        CancellationToken cancellationToken = default);
}
