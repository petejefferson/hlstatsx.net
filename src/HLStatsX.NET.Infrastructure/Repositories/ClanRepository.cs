using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class ClanRepository : IClanRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public ClanRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Clan?> GetByIdAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.FirstOrDefaultAsync(c => c.ClanId == clanId, ct);
    }

    public async Task<PagedResult<ClanLeaderboardRow>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var projected = db.Clans
            .Where(c => c.Game == game && !c.IsHidden)
            .Select(c => new
            {
                c.ClanId,
                c.Name,
                c.Tag,
                MemberCount   = c.Players.Count(p => p.HideRanking == 0),
                TotalKills    = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.Kills) ?? 0,
                TotalDeaths   = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.Deaths) ?? 0,
                TotalConnTime = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.ConnectionTime) ?? 0,
                AvgSkill      = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.Skill) ?? 0.0,
                AvgActivity   = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.ActivityScore) ?? 0.0,
            })
            .Where(x => x.MemberCount >= minMembers && x.AvgActivity >= 0);

        var sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("members",    true)  => projected.OrderByDescending(x => x.MemberCount).ThenBy(x => x.Name),
            ("members",    false) => projected.OrderBy(x => x.MemberCount).ThenBy(x => x.Name),
            ("name",       true)  => projected.OrderByDescending(x => x.Name),
            ("name",       false) => projected.OrderBy(x => x.Name),
            ("kills",      true)  => projected.OrderByDescending(x => x.TotalKills).ThenBy(x => x.Name),
            ("kills",      false) => projected.OrderBy(x => x.TotalKills).ThenBy(x => x.Name),
            ("deaths",     true)  => projected.OrderByDescending(x => x.TotalDeaths).ThenBy(x => x.Name),
            ("deaths",     false) => projected.OrderBy(x => x.TotalDeaths).ThenBy(x => x.Name),
            ("skill",      false) => projected.OrderBy(x => x.AvgSkill).ThenBy(x => x.Name),
            _                     => projected.OrderByDescending(x => x.AvgSkill).ThenBy(x => x.Name),
        };

        var total = await sorted.CountAsync(ct);
        var rawItems = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = rawItems.Select(x => new ClanLeaderboardRow
        {
            ClanId              = x.ClanId,
            Name                = x.Name,
            Tag                 = x.Tag,
            MemberCount         = x.MemberCount,
            TotalKills          = x.TotalKills,
            TotalDeaths         = x.TotalDeaths,
            TotalConnectionTime = x.TotalConnTime,
            AvgSkill            = (int)Math.Round(x.AvgSkill),
            AvgActivity         = Math.Round(x.AvgActivity, 2),
        }).ToList();

        return PagedResult<ClanLeaderboardRow>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players
            .Where(p => p.ClanId == clanId && p.HideRanking == 0)
            .OrderByDescending(p => p.Skill)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Clan>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var q = db.Clans
            .Where(c => (game == null || c.Game == game)
                     && (EF.Functions.Like(c.Name, $"%{query}%") || EF.Functions.Like(c.Tag, $"%{query}%")));

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Clan>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Clan>> GetByCountryAsync(string countryCode, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans
            .Where(c => c.Game == game && c.MapRegion == countryCode && !c.IsHidden)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Clan clan, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Clans.Update(clan);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.CountAsync(c => c.Game == game && !c.IsHidden, ct);
    }

    public async Task<ClanSummaryStats?> GetSummaryAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var row = await db.Clans
            .Where(c => c.ClanId == clanId)
            .Select(c => new
            {
                TotalKills          = c.Players.Where(p => p.HideRanking == 0).Sum(p => (long?)p.Kills) ?? 0,
                TotalDeaths         = c.Players.Where(p => p.HideRanking == 0).Sum(p => (long?)p.Deaths) ?? 0,
                TotalHeadshots      = c.Players.Where(p => p.HideRanking == 0).Sum(p => (long?)p.Headshots) ?? 0,
                TotalConnectionTime = c.Players.Where(p => p.HideRanking == 0).Sum(p => (long?)p.ConnectionTime) ?? 0,
                ActiveMemberCount   = c.Players.Count(p => p.HideRanking == 0),
                TotalMemberCount    = c.Players.Count(),
                RawAvgSkill         = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.Skill) ?? 0.0,
                RawAvgActivity      = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.ActivityScore) ?? 0.0,
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        return new ClanSummaryStats(
            row.TotalKills, row.TotalDeaths, row.TotalHeadshots, row.TotalConnectionTime,
            row.ActiveMemberCount, row.TotalMemberCount,
            (int)Math.Round(row.RawAvgSkill),
            Math.Round(row.RawAvgActivity, 2));
    }

    public async Task<ClanFavoriteServer?> GetFavoriteServerAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await (
            from e in db.EventEntries
            join p in db.Players on e.PlayerId equals p.PlayerId
            join s in db.Servers on e.ServerId equals s.ServerId
            where p.ClanId == clanId
            group s by new { s.ServerId, s.Name } into g
            select new { g.Key.ServerId, g.Key.Name, Count = g.Count() }
        )
        .OrderByDescending(x => x.Count)
        .Select(x => new ClanFavoriteServer(x.ServerId, x.Name))
        .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> GetFavoriteMapAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await (
            from e in db.EventEntries
            join p in db.Players on e.PlayerId equals p.PlayerId
            where p.ClanId == clanId && e.Map != null && e.Map != ""
            group e by e.Map into g
            select new { Map = g.Key, Count = g.Count() }
        )
        .OrderByDescending(x => x.Count)
        .Select(x => x.Map)
        .FirstOrDefaultAsync(ct);
    }

    public async Task<ClanFavoriteWeapon?> GetFavoriteWeaponAsync(int clanId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var raw = await (
            from f in db.EventFrags
            join p in db.Players on f.KillerId equals p.PlayerId
            join w in db.Weapons on f.Weapon equals w.Code
            where p.ClanId == clanId && w.Game == game
            group new { f.Headshot } by new { f.Weapon, w.Name } into g
            select new { g.Key.Weapon, g.Key.Name, Kills = g.Count(), Headshots = g.Sum(x => x.Headshot ? 1 : 0) }
        ).ToListAsync(ct);

        return raw
            .OrderByDescending(x => x.Kills).ThenByDescending(x => x.Headshots)
            .Select(x => new ClanFavoriteWeapon(x.Weapon, x.Name))
            .FirstOrDefault();
    }

    public async Task<PagedResult<ClanMemberRow>> GetMembersPagedAsync(
        int clanId, string game, int page, int pageSize, string sortBy, bool desc, long totalClanKills, CancellationToken ct = default)
    {
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();

        long safeKills = totalClanKills == 0 ? 1 : totalClanKills;

        var q = db1.Players
            .Where(p => p.ClanId == clanId && p.HideRanking == 0)
            .Select(p => new
            {
                p.PlayerId, p.LastName, p.Flag, p.Country,
                p.Skill, p.MmRank, p.Kills, p.Deaths,
                p.ConnectionTime, p.ActivityScore,
                RawKpd = p.Deaths == 0 ? (double)p.Kills : (double)p.Kills / p.Deaths,
            });

        var sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("name",     true)  => q.OrderByDescending(x => x.LastName),
            ("name",     false) => q.OrderBy(x => x.LastName),
            ("mmrank",   true)  => q.OrderByDescending(x => x.MmRank).ThenByDescending(x => x.Skill),
            ("mmrank",   false) => q.OrderBy(x => x.MmRank).ThenByDescending(x => x.Skill),
            ("kills",    true)  => q.OrderByDescending(x => x.Kills).ThenByDescending(x => x.Skill),
            ("kills",    false) => q.OrderBy(x => x.Kills).ThenByDescending(x => x.Skill),
            ("deaths",   true)  => q.OrderByDescending(x => x.Deaths).ThenByDescending(x => x.Skill),
            ("deaths",   false) => q.OrderBy(x => x.Deaths).ThenByDescending(x => x.Skill),
            ("kpd",      true)  => q.OrderByDescending(x => x.RawKpd).ThenByDescending(x => x.Skill),
            ("kpd",      false) => q.OrderBy(x => x.RawKpd).ThenByDescending(x => x.Skill),
            ("time",     true)  => q.OrderByDescending(x => x.ConnectionTime).ThenByDescending(x => x.Skill),
            ("time",     false) => q.OrderBy(x => x.ConnectionTime).ThenByDescending(x => x.Skill),
            ("activity", true)  => q.OrderByDescending(x => x.ActivityScore).ThenByDescending(x => x.Skill),
            ("activity", false) => q.OrderBy(x => x.ActivityScore).ThenByDescending(x => x.Skill),
            ("skill",    false) => q.OrderBy(x => x.Skill),
            _                   => q.OrderByDescending(x => x.Skill),
        };

        var totalTask = sorted.CountAsync(ct);
        var rawTask   = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ranksTask = db2.Ranks.Where(r => r.Game == game).ToListAsync(ct);

        await Task.WhenAll(totalTask, rawTask, ranksTask);

        var ranks = ranksTask.Result;

        var rows = rawTask.Result.Select(x =>
        {
            var rank = ranks.FirstOrDefault(r => x.Kills >= r.MinKills && x.Kills <= r.MaxKills);
            return new ClanMemberRow(
                x.PlayerId, x.LastName, x.Flag, x.Country,
                x.Skill, x.MmRank, x.Kills, x.Deaths,
                x.ConnectionTime, x.ActivityScore,
                x.Deaths == 0 ? x.Kills : Math.Round((double)x.Kills / x.Deaths, 2),
                Math.Round((double)x.Kills / safeKills * 100, 2),
                rank?.RankName, rank?.Image);
        }).ToList();

        return PagedResult<ClanMemberRow>.Create(rows, totalTask.Result, page, pageSize);
    }

    public async Task<IReadOnlyList<ClanWeaponRow>> GetWeaponUsageAsync(
        int clanId, string game, long realKills, long realHeadshots, CancellationToken ct = default)
    {
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();

        long safeKills     = realKills     == 0 ? 1 : realKills;
        long safeHeadshots = realHeadshots == 0 ? 1 : realHeadshots;

        var killsTask = (
            from f in db1.EventFrags
            join p in db1.Players on f.KillerId equals p.PlayerId
            where p.ClanId == clanId
            group f by f.Weapon into g
            select new { Weapon = g.Key, Kills = (long)g.Count(), Headshots = (long)g.Sum(x => x.Headshot ? 1 : 0) }
        ).ToListAsync(ct);

        var weaponDefsTask = db2.Weapons
            .Where(w => w.Game == game)
            .Select(w => new { w.Code, w.Name, w.Modifier })
            .ToListAsync(ct);

        await Task.WhenAll(killsTask, weaponDefsTask);

        var weaponDefs = weaponDefsTask.Result.ToDictionary(w => w.Code);

        return killsTask.Result
            .Where(x => weaponDefs.ContainsKey(x.Weapon))
            .OrderByDescending(x => x.Kills)
            .Select(x =>
            {
                var def = weaponDefs[x.Weapon];
                return new ClanWeaponRow(
                    x.Weapon, def.Name, def.Modifier,
                    x.Kills,
                    Math.Round((double)x.Kills      / safeKills     * 100, 2),
                    x.Headshots,
                    Math.Round((double)x.Headshots  / safeHeadshots * 100, 2),
                    x.Kills == 0 ? 0 : Math.Round((double)x.Headshots / x.Kills, 2));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ClanMapRow>> GetMapPerformanceAsync(
        int clanId, long realKills, long realHeadshots, CancellationToken ct = default)
    {
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();

        long safeKills     = realKills     == 0 ? 1 : realKills;
        long safeHeadshots = realHeadshots == 0 ? 1 : realHeadshots;

        var killsTask = (
            from f in db1.EventFrags
            join p in db1.Players on f.KillerId equals p.PlayerId
            where p.ClanId == clanId
            group f by f.Map into g
            select new { Map = g.Key, Kills = (long)g.Count(), Headshots = (long)g.Sum(x => x.Headshot ? 1 : 0) }
        ).ToListAsync(ct);

        var deathsTask = (
            from f in db2.EventFrags
            join p in db2.Players on f.VictimId equals p.PlayerId
            where p.ClanId == clanId
            group f by f.Map into g
            select new { Map = g.Key, Deaths = (long)g.Count() }
        ).ToListAsync(ct);

        await Task.WhenAll(killsTask, deathsTask);

        var deathsDict = deathsTask.Result
            .GroupBy(x => string.IsNullOrEmpty(x.Map) ? "(Unaccounted)" : x.Map)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Deaths));

        return killsTask.Result
            .GroupBy(x => string.IsNullOrEmpty(x.Map) ? "(Unaccounted)" : x.Map)
            .Select(g => new { Map = g.Key, Kills = g.Sum(x => x.Kills), Headshots = g.Sum(x => x.Headshots) })
            .Where(x => deathsDict.ContainsKey(x.Map))
            .OrderByDescending(x => x.Kills)
            .Select(x =>
            {
                long deaths = deathsDict[x.Map];
                return new ClanMapRow(
                    x.Map,
                    x.Kills,
                    deaths,
                    x.Headshots,
                    Math.Round((double)x.Kills     / safeKills     * 100, 2),
                    Math.Round((double)x.Headshots / safeHeadshots * 100, 2),
                    deaths  == 0 ? x.Kills  : Math.Round((double)x.Kills     / deaths,   2),
                    x.Kills == 0 ? 0        : Math.Round((double)x.Headshots / x.Kills,  2));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ClanActionRow>> GetActionsAsync(int clanId, CancellationToken ct = default)
    {
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();

        var paTask = (
            from e in db1.EventPlayerActions
            join p in db1.Players on e.PlayerId equals p.PlayerId
            join a in db1.GameActions on e.ActionId equals a.ActionId
            where p.ClanId == clanId
            group new { a.Code, a.Description, e.Bonus } by new { a.Code, a.Description } into g
            select new { g.Key.Code, g.Key.Description, Count = (long)g.Count(), Bonus = (long)g.Sum(x => x.Bonus) }
        ).ToListAsync(ct);

        var ppaTask = (
            from e in db2.EventPlayerPlayerActions
            join p in db2.Players on e.PlayerId equals p.PlayerId
            join a in db2.GameActions on e.ActionId equals a.ActionId
            where p.ClanId == clanId
            group new { a.Code, a.Description, e.Bonus } by new { a.Code, a.Description } into g
            select new { g.Key.Code, g.Key.Description, Count = (long)g.Count(), Bonus = (long)g.Sum(x => x.Bonus) }
        ).ToListAsync(ct);

        await Task.WhenAll(paTask, ppaTask);

        return paTask.Result.Concat(ppaTask.Result)
            .GroupBy(x => x.Code)
            .Select(g => new ClanActionRow(
                g.Key,
                g.First().Description ?? g.Key,
                g.Sum(x => x.Count),
                g.Sum(x => x.Bonus)))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<ClanActionRow>> GetActionVictimsAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var raw = await (
            from e in db.EventPlayerPlayerActions
            join p in db.Players on e.VictimId equals p.PlayerId
            join a in db.GameActions on e.ActionId equals a.ActionId
            where p.ClanId == clanId
            group new { a.Code, a.Description, e.Bonus } by new { a.Code, a.Description } into g
            select new { g.Key.Code, g.Key.Description, Count = (long)g.Count(), Bonus = (long)g.Sum(x => x.Bonus) }
        ).ToListAsync(ct);

        return raw
            .OrderByDescending(x => x.Count)
            .Select(x => new ClanActionRow(x.Code, x.Description ?? x.Code, x.Count, -x.Bonus))
            .ToList();
    }

    public async Task<IReadOnlyList<ClanTeamRow>> GetTeamSelectionAsync(int clanId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var raw = await (
            from ct2 in db.EventChangeTeams
            join p  in db.Players on ct2.PlayerId equals p.PlayerId
            join t  in db.Teams   on ct2.Team      equals t.Code
            where p.ClanId == clanId && t.Game == game
            group ct2 by t.Name into g
            select new { Name = g.Key, Count = (long)g.Count() }
        ).ToListAsync(ct);

        long total     = raw.Sum(x => x.Count);
        long safeTotal = total == 0 ? 1 : total;

        return raw
            .OrderByDescending(x => x.Count)
            .Select(x => new ClanTeamRow(x.Name, x.Count, Math.Round((double)x.Count / safeTotal * 100, 2)))
            .ToList();
    }

    public async Task<IReadOnlyList<ClanRoleRow>> GetRoleSelectionAsync(int clanId, string game, CancellationToken ct = default)
    {
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();
        await using var db3 = _factory.CreateDbContext();

        // Role-change event counts for clan players on this game
        var roleCountsTask = (
            from cr in db1.EventChangeRoles
            join p  in db1.Players  on cr.PlayerId  equals p.PlayerId
            join s  in db1.Servers  on cr.ServerId  equals s.ServerId
            join r  in db1.Roles    on cr.Role       equals r.Code
            where p.ClanId == clanId && s.Game == game && r.Game == game
            group cr by new { r.Code, r.Name } into g
            select new { g.Key.Code, g.Key.Name, Count = (long)g.Count() }
        ).ToListAsync(ct);

        // Kills by killerRole for clan players
        var killsByRoleTask = (
            from f in db2.EventFrags
            join p in db2.Players on f.KillerId equals p.PlayerId
            join s in db2.Servers on f.ServerId  equals s.ServerId
            where p.ClanId == clanId && s.Game == game
            group f by f.KillerRole into g
            select new { Role = g.Key, Kills = (long)g.Count() }
        ).ToListAsync(ct);

        // Deaths by victimRole for clan players
        var deathsByRoleTask = (
            from f in db3.EventFrags
            join p in db3.Players on f.VictimId equals p.PlayerId
            join s in db3.Servers on f.ServerId  equals s.ServerId
            where p.ClanId == clanId && s.Game == game
            group f by f.VictimRole into g
            select new { Role = g.Key, Deaths = (long)g.Count() }
        ).ToListAsync(ct);

        await Task.WhenAll(roleCountsTask, killsByRoleTask, deathsByRoleTask);

        var killsByRole  = killsByRoleTask.Result.ToDictionary(x => x.Role, x => x.Kills);
        var deathsByRole = deathsByRoleTask.Result.ToDictionary(x => x.Role, x => x.Deaths);

        long total     = roleCountsTask.Result.Sum(x => x.Count);
        long safeTotal = total == 0 ? 1 : total;

        return roleCountsTask.Result
            .OrderByDescending(x => x.Count)
            .Select(x =>
            {
                long kills  = killsByRole.TryGetValue(x.Code,  out var k) ? k : 0;
                long deaths = deathsByRole.TryGetValue(x.Code, out var d) ? d : 0;
                return new ClanRoleRow(
                    x.Code, x.Name,
                    x.Count,
                    Math.Round((double)x.Count / safeTotal * 100, 2),
                    kills, deaths,
                    deaths == 0 ? kills : Math.Round((double)kills / deaths, 2));
            })
            .ToList();
    }
}
