using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

/// <summary>
/// Core player data access: CRUD, leaderboard, search, history, and basic profile lists.
/// Profile-stat queries live in PlayerStatsRepository.
/// </summary>
public class PlayerRepository : IPlayerRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public PlayerRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Player?> GetByIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players
            .Include(p => p.Clan)
            .Include(p => p.UniqueIds)
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);
    }

    public async Task<PagedResult<Player>> GetRankingsAsync(string game, int page, int pageSize,
        string sortBy = "skill", bool descending = true, int minKills = 1, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        IQueryable<Player> query = db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills >= minKills)
            .Include(p => p.Clan)
            .Include(p => p.UniqueIds);

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("skill",          true)  => query.OrderByDescending(p => p.Skill),
            ("skill",          false) => query.OrderBy(p => p.Skill),
            ("kills",          true)  => query.OrderByDescending(p => p.Kills),
            ("kills",          false) => query.OrderBy(p => p.Kills),
            ("deaths",         true)  => query.OrderByDescending(p => p.Deaths),
            ("deaths",         false) => query.OrderBy(p => p.Deaths),
            ("headshots",      true)  => query.OrderByDescending(p => p.Headshots),
            ("headshots",      false) => query.OrderBy(p => p.Headshots),
            ("connectiontime", true)  => query.OrderByDescending(p => p.ConnectionTime),
            ("connectiontime", false) => query.OrderBy(p => p.ConnectionTime),
            ("name",           true)  => query.OrderByDescending(p => p.LastName),
            ("name",           false) => query.OrderBy(p => p.LastName),
            ("clan",           true)  => query.OrderByDescending(p => p.Clan!.Tag),
            ("clan",           false) => query.OrderBy(p => p.Clan!.Tag),
            ("activity",       true)  => query.OrderByDescending(p => p.ActivityScore),
            ("activity",       false) => query.OrderBy(p => p.ActivityScore),
            ("armyrank",       true)  => query.OrderByDescending(p => p.Kills),
            ("armyrank",       false) => query.OrderBy(p => p.Kills),
            ("kd",             true)  => query.OrderByDescending(p => p.Deaths == 0 ? (double)p.Kills : (double)p.Kills / p.Deaths),
            ("kd",             false) => query.OrderBy(p => p.Deaths == 0 ? (double)p.Kills : (double)p.Kills / p.Deaths),
            ("hspct",          true)  => query.OrderByDescending(p => p.Kills == 0 ? 0.0 : (double)p.Headshots / p.Kills),
            ("hspct",          false) => query.OrderBy(p => p.Kills == 0 ? 0.0 : (double)p.Headshots / p.Kills),
            ("hsk",            true)  => query.OrderByDescending(p => p.Kills == 0 ? 0.0 : (double)p.Headshots / p.Kills),
            ("hsk",            false) => query.OrderBy(p => p.Kills == 0 ? 0.0 : (double)p.Headshots / p.Kills),
            ("accuracy",       true)  => query.OrderByDescending(p => p.Shots == 0 ? 0.0 : (double)p.Hits / p.Shots),
            ("accuracy",       false) => query.OrderBy(p => p.Shots == 0 ? 0.0 : (double)p.Hits / p.Shots),
            _                         => query.OrderByDescending(p => p.Skill)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Player>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<PlayerName>> GetAliasesAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.PlayerNames
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.Numuses)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerHistory>> GetHistoryAsync(int playerId, int days = 30, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await db.PlayerHistories
            .Where(h => h.PlayerId == playerId && h.EventTime >= cutoff)
            .OrderBy(h => h.EventTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerAward>> GetAwardsAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.PlayerAwards
            .Include(a => a.Award)
            .Where(a => a.PlayerId == playerId)
            .OrderByDescending(a => a.Count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerRibbon>> GetRibbonsAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.PlayerRibbons
            .Include(r => r.Ribbon)
            .Where(r => r.PlayerId == playerId)
            .ToListAsync(ct);
    }

    public async Task<int> GetRankAsync(int playerId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var player = await db.Players.FindAsync(new object[] { playerId }, ct);
        if (player is null) return 0;

        // Count how many players have more skill — add 1 for 1-based rank
        return await db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills > 0 && p.Skill > player.Skill)
            .CountAsync(ct) + 1;
    }

    public async Task<PagedResult<PlayerSearchResult>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Search all aliases (PlayerNames) like the PHP site does — not just the current LastName
        var q = from pn in db.PlayerNames
                join p  in db.Players on pn.PlayerId equals p.PlayerId
                join g  in db.Games   on p.Game      equals g.Code
                where EF.Functions.Like(pn.Name, $"%{query}%")
                   && (game == null || p.Game == game)
                   && g.Hidden != "1"
                orderby pn.Name
                select new PlayerSearchResult(p.PlayerId, pn.Name, p.Flag, p.Country, g.Name);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        if (items.Count > 0)
        {
            var playerIds = items.Select(r => r.PlayerId).Distinct().ToList();
            var botIds = await db.PlayerUniqueIds
                .Where(u => playerIds.Contains(u.PlayerId) && EF.Functions.Like(u.UniqueId, "BOT%"))
                .Select(u => u.PlayerId)
                .ToHashSetAsync(ct);
            items = items.Select(r => r with { IsBot = botIds.Contains(r.PlayerId) }).ToList();
        }

        return PagedResult<PlayerSearchResult>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<UniqueIdSearchResult>> SearchByUniqueIdAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var q = from u in db.PlayerUniqueIds
                join p in db.Players on u.PlayerId equals p.PlayerId
                join g in db.Games on u.Game equals g.Code
                where g.Hidden != "1"
                   && EF.Functions.Like(u.UniqueId, $"%{query}%")
                   && (game == null || u.Game == game)
                orderby u.UniqueId
                select new UniqueIdSearchResult(p.PlayerId, u.UniqueId, p.LastName, p.Flag, p.Country, g.Name);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<UniqueIdSearchResult>.Create(items, total, page, pageSize);
    }

    // HLStatsX uses hideranking=1 to "ban"/hide players from rankings
    public async Task<IReadOnlyList<Player>> GetBannedAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players
            .Where(p => p.Game == game && p.HideRanking == 1)
            .OrderByDescending(p => p.LastEvent)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, int count = 50, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.PlayerHistories
            .Where(h => h.Game == game)
            .Select(h => h.EventTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<PlayerLeaderboardRow>> GetHistoryRankingsAsync(
        string game, DateTime from, DateTime to,
        int page, int pageSize, string sortBy = "kills", bool descending = true,
        int minKills = 1, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var histAgg = db.PlayerHistories
            .Where(h => h.Game == game && h.EventTime >= from && h.EventTime < to)
            .GroupBy(h => h.PlayerId)
            .Select(g => new
            {
                PlayerId  = g.Key,
                Points    = g.Sum(h => h.SkillChange),
                Kills     = g.Sum(h => h.Kills),
                Deaths    = g.Sum(h => h.Deaths),
                Headshots = g.Sum(h => h.Headshots),
                ConnTime  = g.Sum(h => h.ConnectionTime)
            });

        var joined =
            from agg  in histAgg
            join p    in db.Players on agg.PlayerId equals p.PlayerId
            where p.HideRanking == 0 && agg.Kills >= minKills
            join c    in db.Clans on p.ClanId equals c.ClanId into cg
            from clan in cg.DefaultIfEmpty()
            select new
            {
                agg.PlayerId,
                p.LastName,
                p.Flag,
                p.Country,
                Clan         = clan,
                p.ActivityScore,
                AllTimeKills = p.Kills,
                agg.Points,
                PeriodKills  = agg.Kills,
                agg.Deaths,
                agg.Headshots,
                ConnTime     = agg.ConnTime
            };

        var ordered = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("kills",          true)  => joined.OrderByDescending(x => x.PeriodKills),
            ("kills",          false) => joined.OrderBy(x => x.PeriodKills),
            ("deaths",         true)  => joined.OrderByDescending(x => x.Deaths),
            ("deaths",         false) => joined.OrderBy(x => x.Deaths),
            ("headshots",      true)  => joined.OrderByDescending(x => x.Headshots),
            ("headshots",      false) => joined.OrderBy(x => x.Headshots),
            ("connectiontime", true)  => joined.OrderByDescending(x => x.ConnTime),
            ("connectiontime", false) => joined.OrderBy(x => x.ConnTime),
            ("name",           true)  => joined.OrderByDescending(x => x.LastName),
            ("name",           false) => joined.OrderBy(x => x.LastName),
            ("clan",           true)  => joined.OrderByDescending(x => x.Clan!.Tag),
            ("clan",           false) => joined.OrderBy(x => x.Clan!.Tag),
            ("activity",       true)  => joined.OrderByDescending(x => x.ActivityScore),
            ("activity",       false) => joined.OrderBy(x => x.ActivityScore),
            ("armyrank",       true)  => joined.OrderByDescending(x => x.AllTimeKills),
            ("armyrank",       false) => joined.OrderBy(x => x.AllTimeKills),
            ("kd",             true)  => joined.OrderByDescending(x => x.Deaths == 0 ? (double)x.PeriodKills : (double)x.PeriodKills / x.Deaths),
            ("kd",             false) => joined.OrderBy(x => x.Deaths == 0 ? (double)x.PeriodKills : (double)x.PeriodKills / x.Deaths),
            ("hsk",            true)  => joined.OrderByDescending(x => x.PeriodKills == 0 ? 0.0 : (double)x.Headshots / x.PeriodKills),
            ("hsk",            false) => joined.OrderBy(x => x.PeriodKills == 0 ? 0.0 : (double)x.Headshots / x.PeriodKills),
            _                         => joined.OrderByDescending(x => x.Points)
        };

        var total = await ordered.CountAsync(ct);
        var raw   = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var botIds = new HashSet<int>();
        if (raw.Count > 0)
        {
            var playerIds = raw.Select(x => x.PlayerId).ToList();
            botIds = await db.PlayerUniqueIds
                .Where(u => playerIds.Contains(u.PlayerId) && EF.Functions.Like(u.UniqueId, "BOT%"))
                .Select(u => u.PlayerId)
                .ToHashSetAsync(ct);
        }

        var items = raw.Select(x => new PlayerLeaderboardRow
        {
            PlayerId       = x.PlayerId,
            LastName       = x.LastName,
            Flag           = x.Flag,
            Country        = x.Country,
            Clan           = x.Clan,
            ActivityScore  = x.ActivityScore,
            AllTimeKills   = x.AllTimeKills,
            Points         = x.Points,
            Kills          = x.PeriodKills,
            Deaths         = x.Deaths,
            Headshots      = x.Headshots,
            ConnectionTime = x.ConnTime,
            Shots          = 0,   // not recorded in history table
            Hits           = 0,   // not recorded in history table
            IsBot          = botIds.Contains(x.PlayerId)
        }).ToList();

        return PagedResult<PlayerLeaderboardRow>.Create(items, total, page, pageSize);
    }

    public async Task<Player?> GetBySteamIdAsync(string steamId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.PlayerUniqueIds
            .Where(u => u.UniqueId == steamId && u.Game == game)
            .Select(u => u.Player)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Players.Update(player);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players.CountAsync(p => p.Game == game, ct);
    }

    public async Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players.Where(p => p.Game == game).SumAsync(p => (long)p.Kills, ct);
    }

    public async Task<IReadOnlyList<TrendPoint>> GetTrendAsync(int playerId, int limit = 30, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        // Take the most recent `limit` entries then reorder ascending for chart display
        return await db.PlayerHistories
            .Where(h => h.PlayerId == playerId)
            .OrderByDescending(h => h.EventTime)
            .Take(limit)
            .OrderBy(h => h.EventTime)
            .Select(h => new TrendPoint(h.EventTime, h.Skill, h.SkillChange))
            .ToListAsync(ct);
    }
}
