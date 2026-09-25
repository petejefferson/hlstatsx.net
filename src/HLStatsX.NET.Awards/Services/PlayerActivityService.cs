using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoInactive() from hlstats-awards.pl:
// - calculates activity score per player based on time since last event
// - sets hideranking=3 for players whose activity drops below 0 (inactive)
public class PlayerActivityService : IPlayerActivityService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly ILogger<PlayerActivityService> _logger;

    public PlayerActivityService(IDbContextFactory<HLStatsDbContext> factory, ILogger<PlayerActivityService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<PlayerActivityResult> UpdateAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var minActivityDays = await GetOptionIntAsync(db, "MinActivity", 28, ct);
        var minActivitySeconds = minActivityDays * 86400;

        if (minActivitySeconds <= 0)
        {
            _logger.LogInformation("MinActivity is 0 — skipping activity update");
            return new PlayerActivityResult(minActivityDays, false);
        }

        var useTimestamp = await GetOptionIntAsync(db, "UseTimestamp", 0, ct);

        if (useTimestamp > 0)
        {
            await UpdateActivityByGameTimestampAsync(db, minActivitySeconds, ct);
        }
        else
        {
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE hlstats_Players
                  SET activity = IF(({0} > UNIX_TIMESTAMP() - last_event),
                      ((100.0 / {0}) * ({0} - (UNIX_TIMESTAMP() - last_event))),
                      -1)",
                new object[] { minActivitySeconds }, ct);
        }

        var deactivated = await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Players SET hideranking = 3 WHERE hideranking = 0 AND activity < 0",
            ct);

        _logger.LogInformation("Activity updated — {Deactivated} players deactivated", deactivated);
        return new PlayerActivityResult(minActivityDays, useTimestamp > 0);
    }

    private static async Task UpdateActivityByGameTimestampAsync(HLStatsDbContext db, int minActivitySeconds, CancellationToken ct)
    {
        var perGame = await db.Servers
            .GroupBy(s => s.Game)
            .Select(g => new { Game = g.Key, LastEvent = g.Max(s => s.LastEvent) })
            .ToListAsync(ct);

        foreach (var row in perGame)
        {
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE hlstats_Players
                  SET activity = IF(({0} > {1} - last_event),
                      ((100.0 / {0}) * ({0} - ({1} - last_event))),
                      -1)
                  WHERE game = {2}",
                new object[] { minActivitySeconds, row.LastEvent, row.Game }, ct);
        }
    }

    private static async Task<int> GetOptionIntAsync(HLStatsDbContext db, string key, int defaultValue, CancellationToken ct)
    {
        var raw = await db.Options
            .Where(o => o.KeyName == key)
            .Select(o => o.Value)
            .SingleOrDefaultAsync(ct);
        return int.TryParse(raw, out var val) ? val : defaultValue;
    }
}
