using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class MapRepository : IMapRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public MapRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<MapCount?> GetByNameAsync(string mapName, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.MapCounts.FirstOrDefaultAsync(m => m.Map == mapName && m.Game == game, ct);
    }

    public async Task<PagedResult<MapCount>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.MapCounts.Where(m => m.Game == game);

        query = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("map",        true)  => query.OrderByDescending(m => m.Map),
            ("map",        false) => query.OrderBy(m => m.Map),
            ("headshots",  true)  => query.OrderByDescending(m => m.Headshots),
            ("headshots",  false) => query.OrderBy(m => m.Headshots),
            ("hpk",        true)  => query.OrderByDescending(m => m.Kills == 0 ? 0.0 : (double)m.Headshots / m.Kills),
            ("hpk",        false) => query.OrderBy(m => m.Kills == 0 ? 0.0 : (double)m.Headshots / m.Kills),
            (_,            true)  => query.OrderByDescending(m => m.Kills),
            (_,            false) => query.OrderBy(m => m.Kills)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<MapCount>.Create(items, total, page, pageSize);
    }

    public async Task<(long TotalKills, long TotalHeadshots)> GetKillTotalsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var totals = await db.MapCounts
            .Where(m => m.Game == game)
            .GroupBy(_ => 1)
            .Select(g => new { Kills = (long?)g.Sum(m => (long)m.Kills), Headshots = (long?)g.Sum(m => (long)m.Headshots) })
            .FirstOrDefaultAsync(ct);
        return (totals?.Kills ?? 0, totals?.Headshots ?? 0);
    }

    public async Task<IReadOnlyList<MapCount>> GetTopMapsAsync(string game, int count = 10, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.MapCounts
            .Where(m => m.Game == game)
            .OrderByDescending(m => m.Kills)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<MapPlayerRow>> GetPlayerLeaderboardAsync(string map, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var grouped = db.EventFrags
            .Join(db.Players, f => f.KillerId, p => p.PlayerId, (f, p) => new
            {
                f.Map,
                p.PlayerId,
                p.LastName,
                p.Flag,
                p.Game,
                p.HideRanking,
                f.Headshot
            })
            .Where(x => x.Map == map && x.Game == game && x.HideRanking == 0)
            .GroupBy(x => new { x.PlayerId, x.LastName, x.Flag })
            .Select(g => new
            {
                g.Key.PlayerId,
                g.Key.LastName,
                g.Key.Flag,
                Kills = (long)g.Count(),
                Headshots = g.Sum(x => x.Headshot ? 1L : 0L)
            });

        grouped = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("name",       true)  => grouped.OrderByDescending(x => x.LastName),
            ("name",       false) => grouped.OrderBy(x => x.LastName),
            ("headshots",  true)  => grouped.OrderByDescending(x => x.Headshots),
            ("headshots",  false) => grouped.OrderBy(x => x.Headshots),
            ("hpk",        true)  => grouped.OrderByDescending(x => x.Kills == 0 ? 0.0 : (double)x.Headshots / x.Kills),
            ("hpk",        false) => grouped.OrderBy(x => x.Kills == 0 ? 0.0 : (double)x.Headshots / x.Kills),
            (_,            true)  => grouped.OrderByDescending(x => x.Kills),
            (_,            false) => grouped.OrderBy(x => x.Kills),
        };

        var total = await grouped.CountAsync(ct);
        var rows = await grouped.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = rows.Select(x => new MapPlayerRow(
            x.PlayerId, x.LastName, x.Flag, x.Kills, x.Headshots,
            x.Kills == 0 ? 0 : Math.Round((double)x.Headshots / x.Kills, 2)
        )).ToList();

        return PagedResult<MapPlayerRow>.Create(items, total, page, pageSize);
    }

    public async Task<long> GetMapTotalKillsAsync(string map, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventFrags
            .Join(db.Servers, f => f.ServerId, s => s.ServerId, (f, s) => new { f.Map, s.Game })
            .Where(x => x.Map == map && x.Game == game)
            .LongCountAsync(ct);
    }
}
