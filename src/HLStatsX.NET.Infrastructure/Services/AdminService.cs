using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace HLStatsX.NET.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _admin;

    public AdminService(IAdminRepository admin) => _admin = admin;

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task<AdminUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _admin.GetByUsernameAsync(username, ct);
        if (user is null) return null;
        var hash = Md5(password);
        return string.Equals(user.Password, hash, StringComparison.OrdinalIgnoreCase) ? user : null;
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default) =>
        _admin.GetAllAsync(ct);

    public async Task CreateUserAsync(AdminUser user, string password, CancellationToken ct = default)
    {
        user.Password = Md5(password);
        await _admin.AddAsync(user, ct);
    }

    public async Task UpdateUserAsync(AdminUser user, string? newPassword, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(newPassword))
            user.Password = Md5(newPassword);
        await _admin.UpdateAsync(user, ct);
    }

    public Task DeleteUserAsync(string username, CancellationToken ct = default) =>
        _admin.DeleteAsync(username, ct);

    // ── Options ───────────────────────────────────────────────────────────────

    public async Task<Dictionary<string, string>> GetOptionsAsync(CancellationToken ct = default)
    {
        var options = await _admin.GetOptionsAsync(ct);
        return options.ToDictionary(o => o.KeyName, o => o.Value);
    }

    public Task SetOptionAsync(string keyName, string value, CancellationToken ct = default) =>
        _admin.SetOptionAsync(keyName, value, ct);

    public async Task SaveOptionsAsync(Dictionary<string, string> values, CancellationToken ct = default)
    {
        foreach (var (key, value) in values)
            await _admin.SetOptionAsync(key, value, ct);
    }

    // ── Games ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<GameSupported>> GetSupportedGamesAsync(CancellationToken ct = default) =>
        _admin.GetSupportedGamesAsync(ct);

    public Task AddGameAsync(Game game, CancellationToken ct = default) => _admin.AddGameAsync(game, ct);
    public Task UpdateGameAsync(Game game, CancellationToken ct = default) => _admin.UpdateGameAsync(game, ct);
    public Task DeleteGameAsync(string code, CancellationToken ct = default) => _admin.DeleteGameAsync(code, ct);

    // ── Servers ───────────────────────────────────────────────────────────────

    public Task<Server?> GetServerByIdAsync(int id, CancellationToken ct = default) => _admin.GetServerByIdAsync(id, ct);
    public Task AddServerAsync(Server server, CancellationToken ct = default) => _admin.AddServerAsync(server, ct);
    public Task UpdateServerAsync(Server server, CancellationToken ct = default) => _admin.UpdateServerAsync(server, ct);
    public Task DeleteServerAsync(int id, CancellationToken ct = default) => _admin.DeleteServerAsync(id, ct);
    public Task<IReadOnlyList<ServerConfig>> GetServerConfigAsync(int serverId, CancellationToken ct = default) => _admin.GetServerConfigAsync(serverId, ct);
    public Task SetServerConfigAsync(int serverId, string parameter, string value, CancellationToken ct = default) => _admin.SetServerConfigAsync(serverId, parameter, value, ct);
    public Task CopyServerConfigAsync(int fromServerId, int toServerId, CancellationToken ct = default) => _admin.CopyServerConfigAsync(fromServerId, toServerId, ct);
    public Task ResetServerConfigToDefaultsAsync(int serverId, string game, CancellationToken ct = default) => _admin.ResetServerConfigToDefaultsAsync(serverId, game, ct);

    // ── Teams ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default) => _admin.GetTeamsAsync(game, ct);
    public Task<Team?> GetTeamByIdAsync(int id, CancellationToken ct = default) => _admin.GetTeamByIdAsync(id, ct);
    public Task AddTeamAsync(Team team, CancellationToken ct = default) => _admin.AddTeamAsync(team, ct);
    public Task UpdateTeamAsync(Team team, CancellationToken ct = default) => _admin.UpdateTeamAsync(team, ct);
    public Task DeleteTeamAsync(int id, CancellationToken ct = default) => _admin.DeleteTeamAsync(id, ct);

    // ── Roles ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default) => _admin.GetRolesAsync(game, ct);
    public Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default) => _admin.GetRoleByIdAsync(id, ct);
    public Task AddRoleAsync(Role role, CancellationToken ct = default) => _admin.AddRoleAsync(role, ct);
    public Task UpdateRoleAsync(Role role, CancellationToken ct = default) => _admin.UpdateRoleAsync(role, ct);
    public Task DeleteRoleAsync(int id, CancellationToken ct = default) => _admin.DeleteRoleAsync(id, ct);

    // ── Weapons ───────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Weapon>> GetWeaponsAsync(string game, CancellationToken ct = default) => _admin.GetWeaponsAsync(game, ct);
    public Task<Weapon?> GetWeaponByIdAsync(int id, CancellationToken ct = default) => _admin.GetWeaponByIdAsync(id, ct);
    public Task AddWeaponAsync(Weapon weapon, CancellationToken ct = default) => _admin.AddWeaponAsync(weapon, ct);
    public Task UpdateWeaponAsync(Weapon weapon, CancellationToken ct = default) => _admin.UpdateWeaponAsync(weapon, ct);
    public Task DeleteWeaponAsync(int id, CancellationToken ct = default) => _admin.DeleteWeaponAsync(id, ct);

    // ── Actions ───────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default) => _admin.GetActionsAsync(game, ct);
    public Task<GameAction?> GetActionByIdAsync(int id, CancellationToken ct = default) => _admin.GetActionByIdAsync(id, ct);
    public Task AddActionAsync(GameAction action, CancellationToken ct = default) => _admin.AddActionAsync(action, ct);
    public Task UpdateActionAsync(GameAction action, CancellationToken ct = default) => _admin.UpdateActionAsync(action, ct);
    public Task DeleteActionAsync(int id, CancellationToken ct = default) => _admin.DeleteActionAsync(id, ct);

    // ── Ranks ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default) => _admin.GetRanksAsync(game, ct);
    public Task<Rank?> GetRankByIdAsync(int id, CancellationToken ct = default) => _admin.GetRankByIdAsync(id, ct);
    public Task AddRankAsync(Rank rank, CancellationToken ct = default) => _admin.AddRankAsync(rank, ct);
    public Task UpdateRankAsync(Rank rank, CancellationToken ct = default) => _admin.UpdateRankAsync(rank, ct);
    public Task DeleteRankAsync(int id, CancellationToken ct = default) => _admin.DeleteRankAsync(id, ct);

    // ── Ribbons ───────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default) => _admin.GetRibbonsAsync(game, ct);
    public Task<Ribbon?> GetRibbonByIdAsync(int id, CancellationToken ct = default) => _admin.GetRibbonByIdAsync(id, ct);
    public Task AddRibbonAsync(Ribbon ribbon, CancellationToken ct = default) => _admin.AddRibbonAsync(ribbon, ct);
    public Task UpdateRibbonAsync(Ribbon ribbon, CancellationToken ct = default) => _admin.UpdateRibbonAsync(ribbon, ct);
    public Task DeleteRibbonAsync(int id, CancellationToken ct = default) => _admin.DeleteRibbonAsync(id, ct);

    // ── Ribbon triggers ───────────────────────────────────────────────────────

    public Task<IReadOnlyList<RibbonTrigger>> GetRibbonTriggersAsync(string game, CancellationToken ct = default) => _admin.GetRibbonTriggersAsync(game, ct);
    public Task<RibbonTrigger?> GetRibbonTriggerByIdAsync(int id, CancellationToken ct = default) => _admin.GetRibbonTriggerByIdAsync(id, ct);
    public Task AddRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default) => _admin.AddRibbonTriggerAsync(trigger, ct);
    public Task UpdateRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default) => _admin.UpdateRibbonTriggerAsync(trigger, ct);
    public Task DeleteRibbonTriggerAsync(int id, CancellationToken ct = default) => _admin.DeleteRibbonTriggerAsync(id, ct);

    // ── Awards ────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Award>> GetAwardsAsync(string game, string awardType, CancellationToken ct = default) => _admin.GetAwardsAsync(game, awardType, ct);
    public Task<Award?> GetAwardByIdAsync(int id, CancellationToken ct = default) => _admin.GetAwardByIdAsync(id, ct);
    public Task AddAwardAsync(Award award, CancellationToken ct = default) => _admin.AddAwardAsync(award, ct);
    public Task UpdateAwardAsync(Award award, CancellationToken ct = default) => _admin.UpdateAwardAsync(award, ct);
    public Task DeleteAwardAsync(int id, CancellationToken ct = default) => _admin.DeleteAwardAsync(id, ct);

    // ── Clan tags ─────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<ClanTag>> GetClanTagsAsync(CancellationToken ct = default) => _admin.GetClanTagsAsync(ct);
    public Task<ClanTag?> GetClanTagByIdAsync(int id, CancellationToken ct = default) => _admin.GetClanTagByIdAsync(id, ct);
    public Task AddClanTagAsync(ClanTag tag, CancellationToken ct = default) => _admin.AddClanTagAsync(tag, ct);
    public Task UpdateClanTagAsync(ClanTag tag, CancellationToken ct = default) => _admin.UpdateClanTagAsync(tag, ct);
    public Task DeleteClanTagAsync(int id, CancellationToken ct = default) => _admin.DeleteClanTagAsync(id, ct);

    // ── Host groups ───────────────────────────────────────────────────────────

    public Task<IReadOnlyList<HostGroup>> GetHostGroupsAsync(CancellationToken ct = default) => _admin.GetHostGroupsAsync(ct);
    public Task<HostGroup?> GetHostGroupByIdAsync(int id, CancellationToken ct = default) => _admin.GetHostGroupByIdAsync(id, ct);
    public Task AddHostGroupAsync(HostGroup group, CancellationToken ct = default) => _admin.AddHostGroupAsync(group, ct);
    public Task UpdateHostGroupAsync(HostGroup group, CancellationToken ct = default) => _admin.UpdateHostGroupAsync(group, ct);
    public Task DeleteHostGroupAsync(int id, CancellationToken ct = default) => _admin.DeleteHostGroupAsync(id, ct);

    // ── Player / clan tools ───────────────────────────────────────────────────

    public Task<Player?> GetPlayerForEditAsync(int playerId, CancellationToken ct = default) => _admin.GetPlayerForEditAsync(playerId, ct);
    public Task UpdatePlayerAsync(Player player, CancellationToken ct = default) => _admin.UpdatePlayerAsync(player, ct);
    public Task<IReadOnlyList<(string IpAddress, DateTime LastUsed)>> GetPlayerIpsAsync(int playerId, CancellationToken ct = default) => _admin.GetPlayerIpsAsync(playerId, ct);
    public Task<Clan?> GetClanForEditAsync(int clanId, CancellationToken ct = default) => _admin.GetClanForEditAsync(clanId, ct);
    public Task UpdateClanAsync(Clan clan, CancellationToken ct = default) => _admin.UpdateClanAsync(clan, ct);

    // ── Admin events ──────────────────────────────────────────────────────────

    public Task<IReadOnlyList<AdminEvent>> GetAdminEventsAsync(string? eventType, int page, int pageSize, CancellationToken ct = default) =>
        _admin.GetAdminEventsAsync(eventType, page, pageSize, ct);

    public Task<int> GetAdminEventsCountAsync(string? eventType, CancellationToken ct = default) =>
        _admin.GetAdminEventsCountAsync(eventType, ct);

    // ── Tools ─────────────────────────────────────────────────────────────────

    public Task OptimizeTablesAsync(CancellationToken ct = default) => _admin.OptimizeTablesAsync(ct);
    public Task<IReadOnlyList<string>> ResetStatsAsync(string? game, ResetOptions options, CancellationToken ct = default) => _admin.ResetStatsAsync(game, options, ct);
    public Task<IReadOnlyList<string>> CleanupInactiveAsync(string? game, int minKills, CancellationToken ct = default) => _admin.CleanupInactiveAsync(game, minKills, ct);
    public Task<IReadOnlyList<string>> CopyGameSettingsAsync(string fromGame, string toGame, CancellationToken ct = default) => _admin.CopyGameSettingsAsync(fromGame, toGame, ct);

    // ── Private ───────────────────────────────────────────────────────────────

    private static string Md5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
