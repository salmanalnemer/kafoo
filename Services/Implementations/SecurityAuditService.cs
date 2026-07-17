using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;

namespace Kafo.Web.Services.Implementations;

public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ApplicationDbContext db, ILogger<SecurityAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task WriteAsync(
        HttpContext context,
        string eventType,
        string message,
        bool success,
        string severity = "Information",
        string? actorType = null,
        string? actorId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.SystemLogs.Add(new SystemLog
            {
                EventType = Truncate(eventType, 80),
                Severity = Truncate(severity, 20),
                ActorType = TruncateNullable(actorType, 30),
                ActorId = TruncateNullable(actorId, 80),
                IpAddress = TruncateNullable(context.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = TruncateNullable(context.Request.Headers.UserAgent.ToString(), 500),
                Path = TruncateNullable(context.Request.Path.Value, 500),
                Message = Truncate(message, 2000),
                Success = success,
                CorrelationId = TruncateNullable(context.TraceIdentifier, 64),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to persist security audit event {EventType}", eventType);
        }
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim()[..Math.Min(value.Trim().Length, max)];

    private static string? TruncateNullable(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
