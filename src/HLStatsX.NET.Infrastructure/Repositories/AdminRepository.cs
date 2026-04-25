using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public AdminRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    // ── Admin users ──────────────────────────────────────────────────────────

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.AdminUsers.FindAsync(new object[] { username }, ct);
    }

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.AdminUsers.OrderBy(u => u.Username).ToListAsync(ct);
    }

    public async Task AddAsync(AdminUser user, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.AdminUsers.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string username, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var user = await db.AdminUsers.FindAsync(new object[] { username }, ct);
        if (user is not null) { db.AdminUsers.Remove(user); await db.SaveChangesAsync(ct); }
    }

    // ── Options ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Options.OrderBy(o => o.KeyName).ToListAsync(ct);
    }

    public async Task SetOptionAsync(string keyName, string value, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var option = await db.Options.FindAsync(new object[] { keyName }, ct);
        if (option is not null)
        {
            option.Value = value;
            db.Options.Update(option);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            db.Options.Add(new Option { KeyName = keyName, Value = value });
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<string>> GetOptionChoicesAsync(string keyName, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Database
            .SqlQueryRaw<string>(
                "SELECT `value` FROM hlstats_Options_Choices WHERE keyname = {0} ORDER BY isDefault DESC",
                keyName)
            .ToListAsync(ct);
    }

    // ── Games ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<GameSupported>> GetSupportedGamesAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GamesSupported.OrderBy(g => g.Name).ToListAsync(ct);
    }

    public async Task AddGameAsync(Game game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateGameAsync(Game game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Games.Update(game);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteGameAsync(string code, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        // Cascade delete all game data
        var serverIds = await db.Servers
            .Where(s => s.Game == code)
            .Select(s => s.ServerId)
            .ToListAsync(ct);

        if (serverIds.Count > 0)
        {
            var idList = string.Join(",", serverIds);
            var serverTables = new[]
            {
                "hlstats_Events_Admin", "hlstats_Events_ChangeName", "hlstats_Events_ChangeRole",
                "hlstats_Events_ChangeTeam", "hlstats_Events_Chat", "hlstats_Events_Connects",
                "hlstats_Events_Disconnects", "hlstats_Events_Entries", "hlstats_Events_Frags",
                "hlstats_Events_Latency", "hlstats_Events_PlayerActions",
                "hlstats_Events_PlayerPlayerActions", "hlstats_Events_Rcon",
                "hlstats_Events_Statsme", "hlstats_Events_Statsme2",
                "hlstats_Events_StatsmeLatency", "hlstats_Events_StatsmeTime",
                "hlstats_Events_Suicides", "hlstats_Events_TeamBonuses",
                "hlstats_Events_Teamkills", "hlstats_Servers_Config"
            };
            foreach (var tbl in serverTables)
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM `{tbl}` WHERE serverId IN ({idList})", ct);
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_server_load WHERE server_id IN ({idList})", ct);
        }

        var playerIds = await db.Players
            .Where(p => p.Game == code)
            .Select(p => p.PlayerId)
            .ToListAsync(ct);

        if (playerIds.Count > 0)
        {
            var pidList = string.Join(",", playerIds);
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_PlayerNames WHERE playerId IN ({pidList})", ct);
        }

        var gameTables = new[]
        {
            "hlstats_Actions", "hlstats_Awards", "hlstats_Ribbons", "hlstats_Roles",
            "hlstats_Teams", "hlstats_Weapons", "hlstats_Ranks", "hlstats_Maps_Counts",
            "hlstats_Servers", "hlstats_Players_History", "hlstats_Players_Awards",
            "hlstats_Players_Ribbons", "hlstats_PlayerUniqueIds", "hlstats_Players",
            "hlstats_Clans", "hlstats_Trend"
        };
        foreach (var tbl in gameTables)
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM `{tbl}` WHERE game = {{0}}", code, ct);

        await db.Database.ExecuteSqlRawAsync("DELETE FROM hlstats_Games WHERE code = {0}", code, ct);
    }

    // ── Servers ──────────────────────────────────────────────────────────────

    public async Task<Server?> GetServerByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers.FindAsync(new object[] { id }, ct);
    }

    public async Task AddServerAsync(Server server, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Servers.Add(server);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateServerAsync(Server server, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Servers.Update(server);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteServerAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var s = await db.Servers.FindAsync(new object[] { id }, ct);
        if (s is not null) { db.Servers.Remove(s); await db.SaveChangesAsync(ct); }
    }

    public async Task<IReadOnlyList<ServerConfig>> GetServerConfigAsync(int serverId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.ServerConfigs
            .Where(c => c.ServerId == serverId)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServerConfig>> GetServerConfigDefaultsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Database
            .SqlQueryRaw<ServerConfig>(
                "SELECT serverConfigId = 0, serverId = 0, parameter, value FROM hlstats_Servers_Config_Default WHERE game = {0}",
                game)
            .ToListAsync(ct);
    }

    public async Task SetServerConfigAsync(int serverId, string parameter, string value, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var existing = await db.ServerConfigs
            .FirstOrDefaultAsync(c => c.ServerId == serverId && c.ConfigKey == parameter, ct);
        if (existing is not null)
        {
            existing.ConfigValue = value;
            db.ServerConfigs.Update(existing);
        }
        else
        {
            db.ServerConfigs.Add(new ServerConfig { ServerId = serverId, ConfigKey = parameter, ConfigValue = value });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task CopyServerConfigAsync(int fromServerId, int toServerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM hlstats_Servers_Config WHERE serverId = {0}", toServerId, ct);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Servers_Config (serverId, parameter, value) " +
            "SELECT {0}, parameter, value FROM hlstats_Servers_Config WHERE serverId = {1}",
            toServerId, fromServerId, ct);
    }

    public async Task ResetServerConfigToDefaultsAsync(int serverId, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM hlstats_Servers_Config WHERE serverId = {0}", serverId, ct);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Servers_Config (serverId, parameter, value) " +
            "SELECT {0}, parameter, value FROM hlstats_Servers_Config_Default WHERE game = {1}",
            serverId, game, ct);
    }

    // ── Teams ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Teams.Where(t => t.Game == game).OrderBy(t => t.Code).ToListAsync(ct);
    }

    public async Task<Team?> GetTeamByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Teams.FindAsync(new object[] { id }, ct);
    }

    public async Task AddTeamAsync(Team team, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateTeamAsync(Team team, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Teams.Update(team);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteTeamAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var t = await db.Teams.FindAsync(new object[] { id }, ct);
        if (t is not null) { db.Teams.Remove(t); await db.SaveChangesAsync(ct); }
    }

    // ── Roles ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Roles.Where(r => r.Game == game).OrderBy(r => r.Code).ToListAsync(ct);
    }

    public async Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Roles.FindAsync(new object[] { id }, ct);
    }

    public async Task AddRoleAsync(Role role, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Roles.Update(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRoleAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var r = await db.Roles.FindAsync(new object[] { id }, ct);
        if (r is not null) { db.Roles.Remove(r); await db.SaveChangesAsync(ct); }
    }

    // ── Weapons ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Weapon>> GetWeaponsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Weapons.Where(w => w.Game == game).OrderBy(w => w.Code).ToListAsync(ct);
    }

    public async Task<Weapon?> GetWeaponByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Weapons.FindAsync(new object[] { id }, ct);
    }

    public async Task AddWeaponAsync(Weapon weapon, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateWeaponAsync(Weapon weapon, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Weapons.Update(weapon);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteWeaponAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var w = await db.Weapons.FindAsync(new object[] { id }, ct);
        if (w is not null) { db.Weapons.Remove(w); await db.SaveChangesAsync(ct); }
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GameActions.Where(a => a.Game == game).OrderBy(a => a.Code).ToListAsync(ct);
    }

    public async Task<GameAction?> GetActionByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GameActions.FindAsync(new object[] { id }, ct);
    }

    public async Task AddActionAsync(GameAction action, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.GameActions.Add(action);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateActionAsync(GameAction action, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.GameActions.Update(action);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteActionAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var a = await db.GameActions.FindAsync(new object[] { id }, ct);
        if (a is not null) { db.GameActions.Remove(a); await db.SaveChangesAsync(ct); }
    }

    // ── Ranks ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ranks.Where(r => r.Game == game).OrderBy(r => r.MinKills).ToListAsync(ct);
    }

    public async Task<Rank?> GetRankByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ranks.FindAsync(new object[] { id }, ct);
    }

    public async Task AddRankAsync(Rank rank, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Ranks.Add(rank);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRankAsync(Rank rank, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Ranks.Update(rank);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRankAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var r = await db.Ranks.FindAsync(new object[] { id }, ct);
        if (r is not null) { db.Ranks.Remove(r); await db.SaveChangesAsync(ct); }
    }

    // ── Ribbons ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ribbons.Where(r => r.Game == game).OrderBy(r => r.RibbonName).ToListAsync(ct);
    }

    public async Task<Ribbon?> GetRibbonByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Ribbons.FindAsync(new object[] { id }, ct);
    }

    public async Task AddRibbonAsync(Ribbon ribbon, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Ribbons.Add(ribbon);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRibbonAsync(Ribbon ribbon, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Ribbons.Update(ribbon);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRibbonAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var r = await db.Ribbons.FindAsync(new object[] { id }, ct);
        if (r is not null) { db.Ribbons.Remove(r); await db.SaveChangesAsync(ct); }
    }

    // ── Ribbon triggers ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RibbonTrigger>> GetRibbonTriggersAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.RibbonTriggers
            .Include(t => t.Ribbon)
            .Where(t => t.Ribbon!.Game == game)
            .OrderBy(t => t.RibbonId)
            .ToListAsync(ct);
    }

    public async Task<RibbonTrigger?> GetRibbonTriggerByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.RibbonTriggers.FindAsync(new object[] { id }, ct);
    }

    public async Task AddRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.RibbonTriggers.Add(trigger);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.RibbonTriggers.Update(trigger);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRibbonTriggerAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var t = await db.RibbonTriggers.FindAsync(new object[] { id }, ct);
        if (t is not null) { db.RibbonTriggers.Remove(t); await db.SaveChangesAsync(ct); }
    }

    // ── Awards ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Award>> GetAwardsAsync(string game, string awardType, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards
            .Where(a => a.Game == game && a.AwardType == awardType)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<Award?> GetAwardByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Awards.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAwardAsync(Award award, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Awards.Add(award);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAwardAsync(Award award, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Awards.Update(award);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAwardAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var a = await db.Awards.FindAsync(new object[] { id }, ct);
        if (a is not null) { db.Awards.Remove(a); await db.SaveChangesAsync(ct); }
    }

    // ── Clan tags ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ClanTag>> GetClanTagsAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.ClanTags.OrderBy(c => c.Pattern).ToListAsync(ct);
    }

    public async Task<ClanTag?> GetClanTagByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.ClanTags.FindAsync(new object[] { id }, ct);
    }

    public async Task AddClanTagAsync(ClanTag tag, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.ClanTags.Add(tag);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateClanTagAsync(ClanTag tag, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.ClanTags.Update(tag);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteClanTagAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var t = await db.ClanTags.FindAsync(new object[] { id }, ct);
        if (t is not null) { db.ClanTags.Remove(t); await db.SaveChangesAsync(ct); }
    }

    // ── Host groups ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HostGroup>> GetHostGroupsAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.HostGroups.OrderBy(h => h.Name).ToListAsync(ct);
    }

    public async Task<HostGroup?> GetHostGroupByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.HostGroups.FindAsync(new object[] { id }, ct);
    }

    public async Task AddHostGroupAsync(HostGroup group, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.HostGroups.Add(group);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateHostGroupAsync(HostGroup group, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.HostGroups.Update(group);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteHostGroupAsync(int id, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var h = await db.HostGroups.FindAsync(new object[] { id }, ct);
        if (h is not null) { db.HostGroups.Remove(h); await db.SaveChangesAsync(ct); }
    }

    // ── Player edit ──────────────────────────────────────────────────────────

    public async Task<Player?> GetPlayerForEditAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players.FindAsync(new object[] { playerId }, ct);
    }

    public async Task UpdatePlayerAsync(Player player, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Players.Update(player);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(string IpAddress, DateTime LastUsed)>> GetPlayerIpsAsync(int playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventConnects
            .Where(e => e.PlayerId == playerId && e.EventTime != null)
            .GroupBy(e => e.IpAddress)
            .Select(g => new { IpAddress = g.Key, LastUsed = g.Max(e => e.EventTime!.Value) })
            .OrderByDescending(x => x.LastUsed)
            .Take(50)
            .AsAsyncEnumerable()
            .Select(x => (x.IpAddress, x.LastUsed))
            .ToListAsync(ct);
    }

    // ── Clan edit ────────────────────────────────────────────────────────────

    public async Task<Clan?> GetClanForEditAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.FindAsync(new object[] { clanId }, ct);
    }

    public async Task UpdateClanAsync(Clan clan, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Clans.Update(clan);
        await db.SaveChangesAsync(ct);
    }

    // ── Admin events ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminEvent>> GetAdminEventsAsync(string? eventType, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var rconQ = db.Database.SqlQueryRaw<AdminEventRow>(
            @"SELECT 'Rcon' AS EventType, eventTime AS EventTime,
                     CONCAT('""', command, '"" from ', remoteIp) AS Message,
                     IFNULL(s.name, 'Unknown') AS ServerName,
                     r.map AS Map
              FROM hlstats_Events_Rcon r
              LEFT JOIN hlstats_Servers s ON s.serverId = r.serverId");

        var adminQ = db.Database.SqlQueryRaw<AdminEventRow>(
            @"SELECT type AS EventType, eventTime AS EventTime,
                     IF(playerName != '', CONCAT('""', playerName, '"": ', message), message) AS Message,
                     IFNULL(s.name, 'Unknown') AS ServerName,
                     a.map AS Map
              FROM hlstats_Events_Admin a
              LEFT JOIN hlstats_Servers s ON s.serverId = a.serverId");

        IQueryable<AdminEventRow> combined = rconQ.Concat(adminQ);
        if (!string.IsNullOrEmpty(eventType))
            combined = combined.Where(e => e.EventType == eventType);

        var rows = await combined
            .OrderByDescending(e => e.EventTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return rows.Select(r => new AdminEvent(r.EventType, r.EventTime, r.Message, r.ServerName, r.Map)).ToList();
    }

    public async Task<int> GetAdminEventsCountAsync(string? eventType, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var rconQ = db.Database.SqlQueryRaw<AdminEventRow>(
            "SELECT 'Rcon' AS EventType, eventTime AS EventTime, '' AS Message, '' AS ServerName, map AS Map FROM hlstats_Events_Rcon");
        var adminQ = db.Database.SqlQueryRaw<AdminEventRow>(
            "SELECT type AS EventType, eventTime AS EventTime, '' AS Message, '' AS ServerName, map AS Map FROM hlstats_Events_Admin");

        IQueryable<AdminEventRow> combined = rconQ.Concat(adminQ);
        if (!string.IsNullOrEmpty(eventType))
            combined = combined.Where(e => e.EventType == eventType);

        return await combined.CountAsync(ct);
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    public async Task OptimizeTablesAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var tables = new[]
        {
            "hlstats_Players", "hlstats_PlayerNames", "hlstats_PlayerUniqueIds",
            "hlstats_Players_History", "hlstats_Players_Awards", "hlstats_Players_Ribbons",
            "hlstats_Clans", "hlstats_Games", "hlstats_Servers", "hlstats_Servers_Config",
            "hlstats_Actions", "hlstats_Teams", "hlstats_Roles", "hlstats_Weapons",
            "hlstats_Ranks", "hlstats_Ribbons", "hlstats_Awards",
            "hlstats_Maps_Counts", "hlstats_Trend", "hlstats_server_load",
            "hlstats_Events_Frags", "hlstats_Events_Connects", "hlstats_Events_Chat",
            "hlstats_Events_Admin", "hlstats_Events_Rcon"
        };
        var tableList = string.Join(", ", tables);
        await db.Database.ExecuteSqlRawAsync($"OPTIMIZE TABLE {tableList}", ct);
        await db.Database.ExecuteSqlRawAsync($"ANALYZE TABLE {tableList}", ct);
    }

    public async Task<IReadOnlyList<string>> ResetStatsAsync(string? game, ResetOptions opts, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var log = new List<string>();
        var gf = string.IsNullOrEmpty(game) ? "" : $" WHERE game='{game}'";

        if (opts.ClearAwards)
        {
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Awards SET d_winner_id=NULL, d_winner_count=NULL, g_winner_id=NULL, g_winner_count=NULL{gf}", ct);
            if (string.IsNullOrEmpty(game))
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM hlstats_Players_Awards", ct);
                await db.Database.ExecuteSqlRawAsync("DELETE FROM hlstats_Players_Ribbons", ct);
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_Players_Awards{gf}", ct);
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_Players_Ribbons{gf}", ct);
            }
            log.Add("Awards cleared.");
        }

        if (opts.ClearHistory)
        {
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_Players_History{gf}", ct);
            log.Add("History cleared.");
        }

        if (opts.ClearSkill)
        {
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Players SET skill=1000{gf}", ct);
            log.Add("Skill reset to 1000.");
        }

        if (opts.ClearCounts)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"UPDATE hlstats_Players SET connection_time=0, kills=0, deaths=0, suicides=0, shots=0, hits=0, headshots=0, last_skill_change=0, kill_streak=0, death_streak=0{gf}", ct);
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Weapons SET kills=0, headshots=0{gf}", ct);
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Actions SET `count`=0{gf}", ct);
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Roles SET picked=0, kills=0, deaths=0{gf}", ct);
            log.Add("Player counts, weapon kills, action counts and role stats reset to 0.");
        }

        if (opts.ClearMapData)
        {
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Maps_Counts SET kills=0, headshots=0{gf}", ct);
            if (!string.IsNullOrEmpty(game))
                await db.Database.ExecuteSqlRawAsync(
                    $"UPDATE hlstats_Servers SET act_map='', act_players=0, map_started=0{gf}", ct);
            log.Add("Map data reset.");
        }

        if (opts.ClearBans)
        {
            await db.Database.ExecuteSqlRawAsync($"UPDATE hlstats_Players SET hideranking=0{gf}", ct);
            log.Add("Bans cleared.");
        }

        if (opts.ClearEvents)
        {
            var evtTables = new[]
            {
                "hlstats_Events_Admin", "hlstats_Events_ChangeName", "hlstats_Events_ChangeRole",
                "hlstats_Events_ChangeTeam", "hlstats_Events_Chat", "hlstats_Events_Connects",
                "hlstats_Events_Disconnects", "hlstats_Events_Entries", "hlstats_Events_Frags",
                "hlstats_Events_Latency", "hlstats_Events_PlayerActions",
                "hlstats_Events_PlayerPlayerActions", "hlstats_Events_Rcon",
                "hlstats_Events_Statsme", "hlstats_Events_Statsme2", "hlstats_Events_Suicides",
                "hlstats_Events_TeamBonuses", "hlstats_Events_Teamkills"
            };
            if (string.IsNullOrEmpty(game))
            {
                foreach (var tbl in evtTables)
                    await db.Database.ExecuteSqlRawAsync($"DELETE FROM `{tbl}`", ct);
            }
            else
            {
                foreach (var tbl in evtTables)
                    await db.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM `{tbl}` USING `{tbl}` INNER JOIN hlstats_Servers ON (`{tbl}`.serverId=hlstats_Servers.serverId) WHERE hlstats_Servers.game='{game}'", ct);
            }
            log.Add("Events cleared.");
        }

        if (opts.DeletePlayers)
        {
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM hlstats_Players{gf}", ct);
            log.Add("Players deleted.");
        }

        return log;
    }

    public async Task<IReadOnlyList<string>> CleanupInactiveAsync(string? game, int minKills, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var log = new List<string>();
        var gf = string.IsNullOrEmpty(game) ? "" : $" AND game='{game}'";

        var deleted = await db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM hlstats_Players WHERE kills < {minKills} AND hideranking=0{gf}", ct);
        log.Add($"Deleted {deleted} inactive players with fewer than {minKills} kills.");

        var emptyClans = await db.Database.ExecuteSqlRawAsync(
            "DELETE c FROM hlstats_Clans c " +
            "LEFT JOIN hlstats_Players p ON p.clan=c.clanId " +
            "WHERE p.playerId IS NULL" +
            (string.IsNullOrEmpty(game) ? "" : $" AND c.game='{game}'"), ct);
        log.Add($"Deleted {emptyClans} empty clans.");

        return log;
    }

    public async Task<IReadOnlyList<string>> CopyGameSettingsAsync(string fromGame, string toGame, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var log = new List<string>();
        var tables = new[]
        {
            ("hlstats_Actions", "game"),
            ("hlstats_Teams", "game"),
            ("hlstats_Roles", "game"),
            ("hlstats_Weapons", "game"),
            ("hlstats_Ranks", "game"),
            ("hlstats_Ribbons", "game"),
            ("hlstats_Awards", "game"),
        };

        foreach (var (tbl, col) in tables)
        {
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM `{tbl}` WHERE {col}='{toGame}'", ct);
            // Get columns dynamically is complex — use INSERT...SELECT with game overridden
            log.Add($"Copied {tbl}.");
        }

        // For each table, copy rows with game replaced
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Actions (game, code, description, team, reward_player, reward_team, for_PlayerActions, for_PlayerPlayerActions, for_TeamActions, for_WorldActions) " +
            $"SELECT '{toGame}', code, description, team, reward_player, reward_team, for_PlayerActions, for_PlayerPlayerActions, for_TeamActions, for_WorldActions FROM hlstats_Actions WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Teams (game, code, name, playerlist_color, playerlist_bgcolor, hidden) " +
            $"SELECT '{toGame}', code, name, playerlist_color, playerlist_bgcolor, hidden FROM hlstats_Teams WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Roles (game, code, name, hidden) " +
            $"SELECT '{toGame}', code, name, hidden FROM hlstats_Roles WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Weapons (game, code, name, modifier) " +
            $"SELECT '{toGame}', code, name, modifier FROM hlstats_Weapons WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Ranks (game, rankName, image, minKills, maxKills) " +
            $"SELECT '{toGame}', rankName, image, minKills, maxKills FROM hlstats_Ranks WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Ribbons (game, ribbonName, image, awardCode, awardCount, special) " +
            $"SELECT '{toGame}', ribbonName, image, awardCode, awardCount, special FROM hlstats_Ribbons WHERE game='{fromGame}'", ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO hlstats_Awards (game, awardType, code, name, verb) " +
            $"SELECT '{toGame}', awardType, code, name, verb FROM hlstats_Awards WHERE game='{fromGame}'", ct);

        log.Add($"All settings copied from {fromGame} to {toGame}.");
        return log;
    }

    // ── Private helper types ─────────────────────────────────────────────────

    private class AdminEventRow
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime EventTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string? Map { get; set; }
    }
}
