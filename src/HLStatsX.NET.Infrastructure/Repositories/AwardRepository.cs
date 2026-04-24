using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class AwardRepository : IAwardRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public AwardRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Award?> GetByIdAsync(int awardId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards.FindAsync(new object[] { awardId }, ct);
    }

    public async Task<IReadOnlyList<Award>> GetAllAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards
            .Where(a => a.Game == game)
            .Include(a => a.DailyWinner)
            .Include(a => a.GlobalWinner)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards
            .Where(a => a.Game == game)
            .Include(a => a.DailyWinner)
            .Include(a => a.GlobalWinner)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ranks.Where(r => r.Game == game).OrderBy(r => r.MinKills).ToListAsync(ct);
    }

    public async Task<Rank?> GetRankForKillsAsync(string game, int kills, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ranks
            .Where(r => r.Game == game && r.MinKills <= kills && (r.MaxKills == 0 || r.MaxKills >= kills))
            .OrderByDescending(r => r.MinKills)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ribbons
            .Where(r => r.Game == game)
            .OrderBy(r => r.RibbonName)
            .ToListAsync(ct);
    }

    public async Task<Ribbon?> GetRibbonByIdAsync(int ribbonId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ribbons
            .FirstOrDefaultAsync(r => r.RibbonId == ribbonId, ct);
    }

    public async Task<IReadOnlyList<RankRow>> GetRanksWithCountsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var rows = await db.Ranks
            .Where(r => r.Game == game)
            .OrderBy(r => r.MinKills)
            .Select(r => new
            {
                Rank = r,
                PlayerCount = db.Players.Count(p =>
                    p.Game == game &&
                    p.Kills >= r.MinKills &&
                    (r.MaxKills == 0 || p.Kills <= r.MaxKills))
            })
            .ToListAsync(ct);

        return rows.Select(x => new RankRow(x.Rank, x.PlayerCount)).ToList();
    }

    public async Task<IReadOnlyList<RibbonRow>> GetRibbonsWithCountsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var rows = await db.Ribbons
            .Where(r => r.Game == game && r.Special == 0)
            .OrderBy(r => r.AwardCount)
            .ThenBy(r => r.RibbonName)
            .ThenBy(r => r.AwardCode)
            .Select(r => new
            {
                Ribbon = r,
                AchievedCount = db.PlayerRibbons.Count(pr => pr.RibbonId == r.RibbonId),
                AwardName = db.Awards
                    .Where(a => a.Game == game && a.Code == r.AwardCode)
                    .Select(a => a.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(x => new RibbonRow(x.Ribbon, x.AchievedCount, x.AwardName)).ToList();
    }

    public async Task<PagedResult<DailyAwardHistoryRow>> GetDailyAwardHistoryAsync(
        int awardId, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.PlayerAwards
            .Where(pa => pa.AwardId == awardId)
            .Join(db.Players, pa => pa.PlayerId, p => p.PlayerId, (pa, p) => new
            {
                pa.PlayerId,
                pa.AwardTime,
                PlayerName = p.LastName,
                p.Flag,
                pa.Count
            });

        var total = await query.CountAsync(ct);

        var ordered = sortBy switch
        {
            "player" => desc ? query.OrderByDescending(r => r.PlayerName) : query.OrderBy(r => r.PlayerName),
            "count"  => desc ? query.OrderByDescending(r => r.Count)      : query.OrderBy(r => r.Count),
            _        => desc ? query.OrderByDescending(r => r.AwardTime)  : query.OrderBy(r => r.AwardTime),
        };

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new DailyAwardHistoryRow(r.PlayerId, r.AwardTime, r.PlayerName, r.Flag, r.Count))
            .ToListAsync(ct);

        return PagedResult<DailyAwardHistoryRow>.Create(rows, total, page, pageSize);
    }
}
