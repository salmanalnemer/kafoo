using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Services.Implementations;

public sealed class SecurityDataCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityDataCleanupService> _logger;

    public SecurityDataCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityDataCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CleanupAsync(stoppingToken);
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            await db.LoginOtpChallenges
                .Where(x => x.ExpiresAtUtc < now.AddDays(-7))
                .ExecuteDeleteAsync(cancellationToken);

            await db.PasswordSetupTokens
                .Where(x => x.ExpiresAtUtc < now.AddDays(-30))
                .ExecuteDeleteAsync(cancellationToken);

            await db.SystemLogs
                .Where(x => x.CreatedAt < now.AddDays(-180))
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security data retention cleanup failed.");
        }
    }
}
