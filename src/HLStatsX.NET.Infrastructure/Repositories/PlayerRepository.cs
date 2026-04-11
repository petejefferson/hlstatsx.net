using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly HLStatsDbContext _db;

    public PlayerRepository(HLStatsDbContext db) => _db = db;

    public async Task<Player?> GetByIdAsync(int playerId, CancellationToken ct = default) =>
        await _db.Players
            .Include(p => p.Clan)
            .Include(p => p.UniqueIds)
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);

    public async Task<PagedResult<Player>> GetRankingsAsync(string game, int page, int pageSize,
        string sortBy = "skill", bool descending = true, CancellationToken ct = default)
    {
        IQueryable<Player> query = _db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills > 0)
            .Include(p => p.Clan);

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

    public async Task<IReadOnlyList<PlayerName>> GetAliasesAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerNames
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.Numuses)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerHistory>> GetHistoryAsync(int playerId, int days = 30, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _db.PlayerHistories
            .Where(h => h.PlayerId == playerId && h.EventTime >= cutoff)
            .OrderBy(h => h.EventTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerAward>> GetAwardsAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerAwards
            .Include(a => a.Award)
            .Where(a => a.PlayerId == playerId)
            .OrderByDescending(a => a.Count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerRibbon>> GetRibbonsAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerRibbons
            .Include(r => r.Ribbon)
            .Where(r => r.PlayerId == playerId)
            .ToListAsync(ct);

    public async Task<int> GetRankAsync(int playerId, string game, CancellationToken ct = default)
    {
        var player = await _db.Players.FindAsync(new object[] { playerId }, ct);
        if (player is null) return 0;

        return await _db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills > 0 && p.Skill > player.Skill)
            .CountAsync(ct) + 1;
    }

    public async Task<PagedResult<PlayerSearchResult>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default)
    {
        // Search all aliases (PlayerNames) like the PHP site does — not just the current LastName
        var q = from pn in _db.PlayerNames
                join p  in _db.Players on pn.PlayerId equals p.PlayerId
                where EF.Functions.Like(pn.Name, $"%{query}%")
                   && (game == null || p.Game == game)
                orderby pn.Name
                select new PlayerSearchResult(p.PlayerId, pn.Name, p.Flag, p.Country, p.Game);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<PlayerSearchResult>.Create(items, total, page, pageSize);
    }

    // HLStatsX uses hideranking=1 to "ban"/hide players from rankings
    public async Task<IReadOnlyList<Player>> GetBannedAsync(string game, CancellationToken ct = default) =>
        await _db.Players
            .Where(p => p.Game == game && p.HideRanking == 1)
            .OrderByDescending(p => p.LastEvent)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, int count = 50, CancellationToken ct = default)
    {
        var dates = await _db.PlayerHistories
            .Where(h => h.Game == game)
            .Select(h => h.EventTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(count)
            .ToListAsync(ct);
        return dates;
    }

    public async Task<PagedResult<PlayerLeaderboardRow>> GetHistoryRankingsAsync(
        string game, DateTime from, DateTime to,
        int page, int pageSize, string sortBy = "kills", bool descending = true,
        CancellationToken ct = default)
    {
        var histAgg = _db.PlayerHistories
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
            join p    in _db.Players on agg.PlayerId equals p.PlayerId
            where p.HideRanking == 0 && agg.Kills > 0
            join c    in _db.Clans on p.ClanId equals c.ClanId into cg
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
            Hits           = 0    // not recorded in history table
        }).ToList();

        return PagedResult<PlayerLeaderboardRow>.Create(items, total, page, pageSize);
    }

    public async Task<Player?> GetBySteamIdAsync(string steamId, string game, CancellationToken ct = default) =>
        await _db.PlayerUniqueIds
            .Where(u => u.UniqueId == steamId && u.Game == game)
            .Select(u => u.Player)
            .FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default) =>
        await _db.Players.CountAsync(p => p.Game == game, ct);

    public async Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default) =>
        await _db.Players.Where(p => p.Game == game).SumAsync(p => (long)p.Kills, ct);

    public async Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default)
    {
        var realKills = await _db.EventFrags.CountAsync(f => f.KillerId == playerId, ct);
        var realDeaths = await _db.EventFrags.CountAsync(f => f.VictimId == playerId, ct);
        var realHeadshots = await _db.EventFrags.CountAsync(f => f.KillerId == playerId && f.Headshot, ct);
        var realTeamkills = await _db.EventTeamkills.CountAsync(f => f.KillerId == playerId, ct);
        double realKpd = realDeaths == 0 ? realKills : Math.Round((double)realKills / realDeaths, 2);
        double realHpk = realKills == 0 ? 0 : Math.Round((double)realHeadshots / realKills, 2);
        return new RealStats(realKills, realDeaths, realHeadshots, realTeamkills, realKpd, realHpk);
    }

    public async Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default)
    {
        var result = await _db.EventLatencies
            .Where(l => l.PlayerId == playerId)
            .GroupBy(l => l.PlayerId)
            .Select(g => new { AvgPing = (int)Math.Round(g.Average(l => (double)l.Ping)), Count = g.Count() })
            .FirstOrDefaultAsync(ct);
        if (result == null || result.Count == 0) return null;
        return new PingStats(result.AvgPing, result.AvgPing / 2);
    }

    public async Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default)
    {
        return await _db.EventConnects
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.Id)
            .Select(e => e.EventTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default)
    {
        return await _db.EventEntries
            .Where(e => e.PlayerId == playerId)
            .Join(_db.Servers, e => e.ServerId, s => s.ServerId, (e, s) => new { e.ServerId, s.Name })
            .GroupBy(x => new { x.ServerId, x.Name })
            .Select(g => new { g.Key.ServerId, g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => new FavoriteServer(x.ServerId, x.Name))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default)
    {
        return await _db.EventEntries
            .Where(e => e.PlayerId == playerId && e.Map != null)
            .GroupBy(e => e.Map)
            .Select(g => new { Map = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Map)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default)
    {
        var top = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Weapon)
            .Select(g => new { Code = g.Key, Kills = g.Count() })
            .OrderByDescending(x => x.Kills)
            .FirstOrDefaultAsync(ct);
        if (top == null) return null;
        var weapon = await _db.Weapons.FirstOrDefaultAsync(w => w.Code == top.Code, ct);
        return new FavoriteWeapon(top.Code, weapon?.Name ?? top.Code);
    }

    public async Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default)
    {
        return await _db.Ranks
            .Where(r => r.Game == game && r.MinKills > kills)
            .OrderBy(r => r.MinKills)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default)
    {
        var allRibbons = await _db.Ribbons
            .Where(r => r.Game == game)
            .OrderBy(r => r.AwardCode).ThenBy(r => r.AwardCount)
            .ToListAsync(ct);
        var earned = await _db.PlayerRibbons
            .Where(pr => pr.PlayerId == playerId && pr.Game == game)
            .Select(pr => pr.RibbonId)
            .ToListAsync(ct);
        var earnedSet = earned.ToHashSet();
        return allRibbons
            .Select(r => new RibbonDisplay(r.RibbonId, r.RibbonName, r.Image, earnedSet.Contains(r.RibbonId)))
            .ToList();
    }

    public async Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, CancellationToken ct = default)
    {
        var killsList = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.VictimId)
            .Select(g => new { VictimId = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .ToListAsync(ct);

        var deathsDict = await _db.EventFrags
            .Where(f => f.VictimId == playerId)
            .GroupBy(f => f.KillerId)
            .Select(g => new { KillerId = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.KillerId, x => x.Deaths, ct);

        var opponentIds = killsList.Where(k => k.Kills >= 5).Select(k => k.VictimId).ToList();
        var names = await _db.Players
            .Where(p => opponentIds.Contains(p.PlayerId))
            .Select(p => new { p.PlayerId, p.LastName })
            .ToDictionaryAsync(p => p.PlayerId, p => p.LastName, ct);

        return killsList
            .Where(k => k.Kills >= 5)
            .Select(k => new KillStatRow(
                k.VictimId,
                names.GetValueOrDefault(k.VictimId, "Unknown"),
                k.Kills,
                deathsDict.GetValueOrDefault(k.VictimId, 0),
                k.Headshots))
            .OrderByDescending(r => r.Kills)
            .ToList();
    }

    public async Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default)
    {
        var killsByMap = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Map)
            .Select(g => new { Map = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .ToListAsync(ct);

        var deathsByMap = await _db.EventFrags
            .Where(f => f.VictimId == playerId)
            .GroupBy(f => f.Map)
            .Select(g => new { Map = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.Map, x => x.Deaths, ct);

        return killsByMap
            .Select(k => new MapStatRow(
                string.IsNullOrEmpty(k.Map) ? "(Unaccounted)" : k.Map,
                k.Kills,
                deathsByMap.GetValueOrDefault(k.Map ?? "", 0),
                k.Headshots))
            .OrderByDescending(r => r.Kills == 0 ? 0 : (double)r.Kills / (r.Deaths == 0 ? 1 : r.Deaths))
            .ToList();
    }

    public async Task<IReadOnlyList<ServerStatRow>> GetServerPerformanceAsync(int playerId, CancellationToken ct = default)
    {
        var killsByServer = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .Join(_db.Servers, f => f.ServerId, s => s.ServerId, (f, s) => new { s.ServerId, s.Name, f.Headshot })
            .GroupBy(x => new { x.ServerId, x.Name })
            .Select(g => new { g.Key.ServerId, g.Key.Name, Kills = g.LongCount(), Headshots = g.LongCount(x => x.Headshot) })
            .ToListAsync(ct);

        var deathsByServer = await _db.EventFrags
            .Where(f => f.VictimId == playerId)
            .GroupBy(f => f.ServerId)
            .Select(g => new { ServerId = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.ServerId, x => x.Deaths, ct);

        return killsByServer
            .Select(k => new ServerStatRow(
                k.ServerId, k.Name, k.Kills,
                deathsByServer.GetValueOrDefault(k.ServerId, 0),
                k.Headshots))
            .OrderByDescending(r => r.Kills)
            .ToList();
    }

    public async Task<IReadOnlyList<WeaponStatRow>> GetWeaponStatsAsync(int playerId, string game, CancellationToken ct = default)
    {
        var frags = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Weapon)
            .Select(g => new { Code = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .ToListAsync(ct);

        var weapons = await _db.Weapons
            .Where(w => w.Game == game)
            .ToDictionaryAsync(w => w.Code, w => w, ct);

        return frags
            .Select(f =>
            {
                weapons.TryGetValue(f.Code, out var w);
                return new WeaponStatRow(f.Code, w?.Name ?? f.Code, w?.Modifier ?? 1.0f, f.Kills, f.Headshots);
            })
            .OrderByDescending(r => r.Kills)
            .ToList();
    }

    public async Task<IReadOnlyList<TeamStatRow>> GetTeamSelectionAsync(int playerId, string game, CancellationToken ct = default)
    {
        var teamJoins = await _db.EventChangeTeams
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.Team)
            .Select(g => new { Team = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        if (!teamJoins.Any()) return Array.Empty<TeamStatRow>();

        int total = teamJoins.Sum(t => t.Count);
        var teamNames = await _db.Teams
            .Where(t => t.Game == game)
            .ToDictionaryAsync(t => t.Code, t => t.Name, ct);

        return teamJoins
            .Select(t => new TeamStatRow(t.Team, teamNames.GetValueOrDefault(t.Team, t.Team), t.Count, total))
            .OrderByDescending(t => t.JoinCount)
            .ToList();
    }

    public async Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default)
    {
        // Non-pvp actions (area capture, objective, etc.)
        var pa = await _db.EventPlayerActions
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        // PvP actions (domination, etc.) where this player is the actor
        var ppa = await _db.EventPlayerPlayerActions
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        var actionIds = pa.Select(x => x.ActionId).Concat(ppa.Select(x => x.ActionId)).Distinct().ToList();
        if (!actionIds.Any()) return Array.Empty<ActionStatRow>();

        var actions = await _db.GameActions
            .Where(a => actionIds.Contains(a.ActionId))
            .Select(a => new { a.ActionId, Label = a.Description ?? a.Code })
            .ToDictionaryAsync(a => a.ActionId, a => a.Label, ct);

        return pa.Concat(ppa)
            .GroupBy(x => x.ActionId)
            .Select(g => new ActionStatRow(
                actions.GetValueOrDefault(g.Key, g.Key.ToString()),
                g.Sum(x => x.Count),
                g.Sum(x => x.Bonus)))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<ActionStatRow>> GetPlayerActionVictimsAsync(int playerId, CancellationToken ct = default)
    {
        var ppa = await _db.EventPlayerPlayerActions
            .Where(e => e.VictimId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        if (!ppa.Any()) return Array.Empty<ActionStatRow>();

        var actionIds = ppa.Select(x => x.ActionId).Distinct().ToList();
        var actions = await _db.GameActions
            .Where(a => actionIds.Contains(a.ActionId))
            .Select(a => new { a.ActionId, Label = a.Description ?? a.Code })
            .ToDictionaryAsync(a => a.ActionId, a => a.Label, ct);

        return ppa
            .Select(g => new ActionStatRow(
                actions.GetValueOrDefault(g.ActionId, g.ActionId.ToString()),
                g.Count,
                g.Bonus * -1))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default)
    {
        var roleJoins = await _db.EventChangeRoles
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        if (!roleJoins.Any()) return Array.Empty<RoleStatRow>();

        int total = roleJoins.Sum(r => r.Count);
        var roles = await _db.Roles
            .Where(r => r.Game == game)
            .ToDictionaryAsync(r => r.Code, r => r, ct);

        var killsByRole = await _db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.KillerRole)
            .Select(g => new { Role = g.Key, Kills = g.LongCount() })
            .ToDictionaryAsync(x => x.Role, x => x.Kills, ct);

        var deathsByRole = await _db.EventFrags
            .Where(f => f.VictimId == playerId)
            .GroupBy(f => f.VictimRole)
            .Select(g => new { Role = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.Role, x => x.Deaths, ct);

        return roleJoins
            .Select(r =>
            {
                roles.TryGetValue(r.Role, out var role);
                return new RoleStatRow(
                    r.Role, r.Role.ToLower(), r.Count, total,
                    killsByRole.GetValueOrDefault(r.Role, 0),
                    deathsByRole.GetValueOrDefault(r.Role, 0));
            })
            .OrderByDescending(r => r.JoinCount)
            .ToList();
    }
}
