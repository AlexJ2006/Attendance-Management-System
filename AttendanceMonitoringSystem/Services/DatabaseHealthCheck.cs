using AttendanceMonitoringSystem.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AttendanceMonitoringSystem.Services;

// Reports whether the database can be reached. Used by /health so
// availability can be monitored and measured (NFR01).
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
