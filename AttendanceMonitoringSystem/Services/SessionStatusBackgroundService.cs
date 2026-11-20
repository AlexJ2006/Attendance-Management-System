using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services;

public class SessionStatusBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionStatusBackgroundService> _logger;

    public SessionStatusBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionStatusBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Session status background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateSessionStatusesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while updating session statuses.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Session status background service stopped.");
    }

    private async Task UpdateSessionStatusesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.Now;

        var sessions = await context.Sessions
            .Where(s =>
                s.Status == SessionStatus.Scheduled ||
                s.Status == SessionStatus.InProgress)
            .ToListAsync(cancellationToken);

        int updatedCount = 0;

        foreach (var session in sessions)
        {
            if (session.EndTime <= now)
            {
                session.Status = SessionStatus.Completed;

                // Completed sessions must no longer have usable codes.
                session.SessionCode = null;
                session.CodeExpiresAt = null;

                updatedCount++;
            }
            else if (session.Status == SessionStatus.Scheduled &&
                     session.StartTime <= now)
            {
                session.Status = SessionStatus.InProgress;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated the statuses of {Count} teaching sessions.",
                updatedCount);
        }
    }
}
