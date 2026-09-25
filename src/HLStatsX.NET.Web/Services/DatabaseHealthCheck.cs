using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HLStatsX.NET.Web.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public DatabaseHealthCheck(IDbContextFactory<HLStatsDbContext> factory)
        => _factory = factory;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
