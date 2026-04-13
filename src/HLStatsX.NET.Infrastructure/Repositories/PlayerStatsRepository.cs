using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

/// <summary>
/// Profile-stat queries for a single player (real stats, kill breakdowns, favourite
/// server/map/weapon, ribbon status, etc.). Kept separate from PlayerRepository so
/// each class stays small and focused.
/// </summary>
public class PlayerStatsRepository : IPlayerStatsRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public PlayerStatsRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default)
    {
        // Use four parallel single-column queries instead of one OR query.
        // WHERE killerId=X OR victimId=X prevents efficient index use on large tables.
        // Each individual query can do a targeted index seek.
        await using var db1 = _factory.CreateDbContext();
        await using var db2 = _factory.CreateDbContext();
        await using var db3 = _factory.CreateDbContext();
        await using var db4 = _factory.CreateDbContext();

        var killsTask     = db1.EventFrags.LongCountAsync(f => f.KillerId == playerId, ct);
        var deathsTask    = db2.EventFrags.LongCountAsync(f => f.VictimId == playerId, ct);
        var headshotsTask = db3.EventFrags.LongCountAsync(f => f.KillerId == playerId && f.Headshot, ct);
        var teamkillsTask = db4.EventTeamkills.LongCountAsync(f => f.KillerId == playerId, ct);

        await Task.WhenAll(killsTask, deathsTask, headshotsTask, teamkillsTask);

        var realKills     = killsTask.Result;
        var realDeaths    = deathsTask.Result;
        var realHeadshots = headshotsTask.Result;
        var realTeamkills = teamkillsTask.Result;

        double realKpd = realDeaths == 0 ? realKills : Math.Round((double)realKills / realDeaths, 2);
        double realHpk = realKills  == 0 ? 0         : Math.Round((double)realHeadshots / realKills, 2);
        return new RealStats(realKills, realDeaths, realHeadshots, realTeamkills, realKpd, realHpk);
    }

    public async Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var result = await db.EventLatencies
            .Where(l => l.PlayerId == playerId)
            .GroupBy(l => l.PlayerId)
            .Select(g => new { AvgPing = (int)Math.Round(g.Average(l => (double)l.Ping)), Count = g.Count() })
            .FirstOrDefaultAsync(ct);
        if (result == null || result.Count == 0) return null;
        return new PingStats(result.AvgPing, result.AvgPing / 2);
    }

    public async Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventConnects
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.Id)
            .Select(e => e.EventTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventEntries
            .Where(e => e.PlayerId == playerId)
            .Join(db.Servers, e => e.ServerId, s => s.ServerId, (e, s) => new { e.ServerId, s.Name })
            .GroupBy(x => new { x.ServerId, x.Name })
            .Select(g => new { g.Key.ServerId, g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => new FavoriteServer(x.ServerId, x.Name))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventEntries
            .Where(e => e.PlayerId == playerId && e.Map != null)
            .GroupBy(e => e.Map)
            .Select(g => new { Map = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Map)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var topCode = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Weapon)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(ct);

        if (topCode is null) return null;

        var name = await db.Weapons
            .Where(w => w.Code == topCode)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(ct);

        return new FavoriteWeapon(topCode, name ?? topCode);
    }

    public async Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ranks
            .Where(r => r.Game == game && r.MinKills > kills)
            .OrderBy(r => r.MinKills)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Fetch all displayable ribbons for the game (special 0 = normal, 2 = also displayable)
        var allRibbons = await db.Ribbons
            .Where(r => r.Game == game && (r.Special == 0 || r.Special == 2))
            .OrderBy(r => r.AwardCode).ThenByDescending(r => r.AwardCount)
            .ToListAsync(ct);

        // Earned ribbon IDs for this player
        var earnedIds = await db.PlayerRibbons
            .Where(pr => pr.PlayerId == playerId && pr.Game == game)
            .Select(pr => pr.RibbonId)
            .ToListAsync(ct);

        var earnedSet = earnedIds.ToHashSet();

        // Deduplicate by AwardCode — one entry per code, earned takes priority.
        // allRibbons is already ordered awardCode ASC, awardCount DESC so the first
        // entry for each code is the highest tier; earned ones bubble up because we
        // check the earnedSet, not position.
        var seen = new HashSet<string?>();
        var result = new List<RibbonDisplay>();
        foreach (var r in allRibbons)
        {
            if (!seen.Add(r.AwardCode)) continue;  // already have an entry for this code

            // If the player has earned ANY ribbon for this awardCode, find the best one
            bool anyEarned = allRibbons
                .Where(x => x.AwardCode == r.AwardCode)
                .Any(x => earnedSet.Contains(x.RibbonId));

            if (anyEarned)
            {
                // Pick the highest-count earned ribbon for this code
                var best = allRibbons
                    .Where(x => x.AwardCode == r.AwardCode && earnedSet.Contains(x.RibbonId))
                    .First(); // already sorted by AwardCount DESC
                result.Add(new RibbonDisplay(best.RibbonId, best.RibbonName, best.Image, Earned: true));
            }
            else
            {
                result.Add(new RibbonDisplay(r.RibbonId, r.RibbonName, r.Image, Earned: false));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<GlobalAwardRow>> GetGlobalAwardsAsync(int playerId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards
            .Where(a => a.Game == game && a.GlobalWinnerId == playerId)
            .OrderBy(a => a.Name)
            .Select(a => new GlobalAwardRow(a.AwardType, a.Code, a.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Apply HAVING in SQL so we only materialise opponents with 5+ kills, not the full grouped set.
        var killsList = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.VictimId)
            .Select(g => new { VictimId = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .Where(x => x.Kills >= 5)
            .ToListAsync(ct);

        if (killsList.Count == 0) return Array.Empty<KillStatRow>();

        var opponentIds = killsList.Select(k => k.VictimId).ToList();

        // Scope deaths to just the relevant opponents — avoids loading every killer who ever hit this player.
        var deathsDict = await db.EventFrags
            .Where(f => f.VictimId == playerId && opponentIds.Contains(f.KillerId))
            .GroupBy(f => f.KillerId)
            .Select(g => new { KillerId = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.KillerId, x => x.Deaths, ct);

        var names = await db.Players
            .Where(p => opponentIds.Contains(p.PlayerId))
            .Select(p => new { p.PlayerId, p.LastName })
            .ToDictionaryAsync(p => p.PlayerId, p => p.LastName, ct);

        var botIds = await db.PlayerUniqueIds
            .Where(u => opponentIds.Contains(u.PlayerId) && EF.Functions.Like(u.UniqueId, "BOT%"))
            .Select(u => u.PlayerId)
            .ToHashSetAsync(ct);

        return killsList
            .Select(k => new KillStatRow(
                k.VictimId,
                names.GetValueOrDefault(k.VictimId, "Unknown"),
                k.Kills,
                deathsDict.GetValueOrDefault(k.VictimId, 0),
                k.Headshots,
                botIds.Contains(k.VictimId)))
            .OrderByDescending(r => r.Kills)
            .ToList();
    }

    public async Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var killsByMap = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Map)
            .Select(g => new { Map = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .ToListAsync(ct);

        var deathsByMap = await db.EventFrags
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
        await using var db = _factory.CreateDbContext();

        var killsByServer = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .Join(db.Servers, f => f.ServerId, s => s.ServerId, (f, s) => new { s.ServerId, s.Name, f.Headshot })
            .GroupBy(x => new { x.ServerId, x.Name })
            .Select(g => new { g.Key.ServerId, g.Key.Name, Kills = g.LongCount(), Headshots = g.LongCount(x => x.Headshot) })
            .ToListAsync(ct);

        var deathsByServer = await db.EventFrags
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
        await using var db = _factory.CreateDbContext();

        var frags = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.Weapon)
            .Select(g => new { Code = g.Key, Kills = g.LongCount(), Headshots = g.LongCount(f => f.Headshot) })
            .ToListAsync(ct);

        var weapons = await db.Weapons
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
        await using var db = _factory.CreateDbContext();

        var teamJoins = await db.EventChangeTeams
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.Team)
            .Select(g => new { Team = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        if (!teamJoins.Any()) return Array.Empty<TeamStatRow>();

        int total     = teamJoins.Sum(t => t.Count);
        var teamNames = await db.Teams
            .Where(t => t.Game == game)
            .ToDictionaryAsync(t => t.Code, t => t.Name, ct);

        return teamJoins
            .Select(t => new TeamStatRow(t.Team, teamNames.GetValueOrDefault(t.Team, t.Team), t.Count, total))
            .OrderByDescending(t => t.JoinCount)
            .ToList();
    }

    public async Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var roleJoins = await db.EventChangeRoles
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        if (!roleJoins.Any()) return Array.Empty<RoleStatRow>();

        int total = roleJoins.Sum(r => r.Count);
        var roles = await db.Roles
            .Where(r => r.Game == game)
            .ToDictionaryAsync(r => r.Code, r => r, ct);

        var killsByRole = await db.EventFrags
            .Where(f => f.KillerId == playerId)
            .GroupBy(f => f.KillerRole)
            .Select(g => new { Role = g.Key, Kills = g.LongCount() })
            .ToDictionaryAsync(x => x.Role, x => x.Kills, ct);

        var deathsByRole = await db.EventFrags
            .Where(f => f.VictimId == playerId)
            .GroupBy(f => f.VictimRole)
            .Select(g => new { Role = g.Key, Deaths = g.LongCount() })
            .ToDictionaryAsync(x => x.Role, x => x.Deaths, ct);

        return roleJoins
            .Select(r =>
            {
                roles.TryGetValue(r.Role, out var role);
                return new RoleStatRow(
                    r.Role, role?.Name ?? r.Role, r.Role.ToLower(), r.Count, total,
                    killsByRole.GetValueOrDefault(r.Role, 0),
                    deathsByRole.GetValueOrDefault(r.Role, 0));
            })
            .OrderByDescending(r => r.JoinCount)
            .ToList();
    }

    public async Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Non-pvp actions (area capture, objective, etc.)
        var pa = await db.EventPlayerActions
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        // PvP actions (domination, etc.) where this player is the actor
        var ppa = await db.EventPlayerPlayerActions
            .Where(e => e.PlayerId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        var actionIds = pa.Select(x => x.ActionId).Concat(ppa.Select(x => x.ActionId)).Distinct().ToList();
        if (!actionIds.Any()) return Array.Empty<ActionStatRow>();

        var actions = await db.GameActions
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
        await using var db = _factory.CreateDbContext();

        var ppa = await db.EventPlayerPlayerActions
            .Where(e => e.VictimId == playerId)
            .GroupBy(e => e.ActionId)
            .Select(g => new { ActionId = g.Key, Count = g.LongCount(), Bonus = (double)g.Sum(e => e.Bonus) })
            .ToListAsync(ct);

        if (!ppa.Any()) return Array.Empty<ActionStatRow>();

        var actionIds = ppa.Select(x => x.ActionId).Distinct().ToList();
        var actions = await db.GameActions
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
}
