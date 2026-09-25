using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TrendEntity = HLStatsX.NET.Core.Entities.Trend;

namespace HLStatsX.NET.Daemon.Trend;

/// <summary>
/// Inserts a snapshot row into <c>hlstats_Trend</c> per game every 299 seconds.
/// Mirrors <c>track_hlstats_trend</c> in <c>hlstats.pl</c>.
/// </summary>
/// <remarks>
/// Only called when <c>TrackStatsTrend</c> option is enabled and the daemon is
/// running in UDP mode (not STDIN import mode).
/// </remarks>
public sealed class TrendTracker
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<TrendTracker> _logger;

    public TrendTracker(IDbContextFactory<HLStatsDbContext> dbFactory, ILogger<TrendTracker> logger)
    {
        _dbFactory = dbFactory;
        _logger    = logger;
    }

    /// <summary>
    /// Writes one trend row per game. Callers are responsible for the 299-second
    /// interval check; this method always inserts when called.
    /// </summary>
    public async Task TrackAsync(long nowUnix, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Games that have at least one configured server
        var serverGames = await db.Servers
            .Select(s => s.Game)
            .Distinct()
            .ToListAsync(ct);

        if (serverGames.Count == 0) return;

        // Total registered players per game (inner-join with server games list)
        var playerCounts = await db.Players
            .Where(p => serverGames.Contains(p.Game))
            .GroupBy(p => p.Game)
            .Select(g => new { Game = g.Key, Players = g.Count() })
            .ToListAsync(ct);

        if (playerCounts.Count == 0) return;

        int timestamp = (int)nowUnix;
        int inserted = 0;

        foreach (var entry in playerCounts)
        {
            var stats = await db.Servers
                .Where(s => s.Game == entry.Game)
                .GroupBy(s => s.Game)
                .Select(g => new
                {
                    Kills      = g.Sum(s => s.Kills),
                    Headshots  = g.Sum(s => s.Headshots),
                    Servers    = g.Count(),
                    ActSlots   = g.Sum(s => s.ActPlayers),
                    MaxSlots   = g.Sum(s => s.MaxPlayers),
                })
                .SingleOrDefaultAsync(ct);

            if (stats is null) continue;

            int actSlots = stats.MaxSlots > 0
                ? Math.Min(stats.ActSlots, stats.MaxSlots)
                : stats.ActSlots;

            db.Trends.Add(new TrendEntity
            {
                Timestamp  = timestamp,
                Game       = entry.Game,
                Players    = entry.Players,
                Kills      = stats.Kills,
                Headshots  = stats.Headshots,
                Servers    = stats.Servers,
                ActSlots   = actSlots,
                MaxSlots   = stats.MaxSlots,
            });

            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogDebug("Trend snapshot inserted for {Count} games at t={Ts}.", inserted, timestamp);
        }
    }
}
