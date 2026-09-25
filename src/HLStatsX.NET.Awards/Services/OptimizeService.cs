using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoOptimize() from hlstats-awards.pl: runs OPTIMIZE TABLE on all tables.
public class OptimizeService : IOptimizeService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly ILogger<OptimizeService> _logger;

    public OptimizeService(IDbContextFactory<HLStatsDbContext> factory, ILogger<OptimizeService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<OptimizeResult> OptimizeAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);

        var tables = new List<string>();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SHOW TABLES";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add(reader.GetString(0));
        }
        finally
        {
            await conn.CloseAsync();
        }

        foreach (var table in tables)
        {
            // Table names come from SHOW TABLES — not user input
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"OPTIMIZE TABLE `{table}`", ct);
#pragma warning restore EF1002
        }

        _logger.LogInformation("Optimized {Count} tables", tables.Count);
        return new OptimizeResult(tables.Count);
    }
}
