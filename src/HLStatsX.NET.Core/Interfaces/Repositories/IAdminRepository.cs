using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IAdminRepository
{
    // Admin users
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(AdminUser user, CancellationToken ct = default);
    Task UpdateAsync(AdminUser user, CancellationToken ct = default);
    Task DeleteAsync(string username, CancellationToken ct = default);

    // Options
    Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default);
    Task SetOptionAsync(string keyName, string value, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetOptionChoicesAsync(string keyName, CancellationToken ct = default);

    // Games
    Task<IReadOnlyList<GameSupported>> GetSupportedGamesAsync(CancellationToken ct = default);
    Task AddGameAsync(Game game, CancellationToken ct = default);
    Task UpdateGameAsync(Game game, CancellationToken ct = default);
    Task DeleteGameAsync(string code, CancellationToken ct = default);

    // Servers
    Task<Server?> GetServerByIdAsync(int id, CancellationToken ct = default);
    Task AddServerAsync(Server server, CancellationToken ct = default);
    Task UpdateServerAsync(Server server, CancellationToken ct = default);
    Task DeleteServerAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ServerConfig>> GetServerConfigAsync(int serverId, CancellationToken ct = default);
    Task<IReadOnlyList<ServerConfig>> GetServerConfigDefaultsAsync(string game, CancellationToken ct = default);
    Task SetServerConfigAsync(int serverId, string parameter, string value, CancellationToken ct = default);
    Task CopyServerConfigAsync(int fromServerId, int toServerId, CancellationToken ct = default);
    Task ResetServerConfigToDefaultsAsync(int serverId, string game, CancellationToken ct = default);

    // Teams
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);
    Task<Team?> GetTeamByIdAsync(int id, CancellationToken ct = default);
    Task AddTeamAsync(Team team, CancellationToken ct = default);
    Task UpdateTeamAsync(Team team, CancellationToken ct = default);
    Task DeleteTeamAsync(int id, CancellationToken ct = default);

    // Roles
    Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default);
    Task AddRoleAsync(Role role, CancellationToken ct = default);
    Task UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task DeleteRoleAsync(int id, CancellationToken ct = default);

    // Weapons
    Task<IReadOnlyList<Weapon>> GetWeaponsAsync(string game, CancellationToken ct = default);
    Task<Weapon?> GetWeaponByIdAsync(int id, CancellationToken ct = default);
    Task AddWeaponAsync(Weapon weapon, CancellationToken ct = default);
    Task UpdateWeaponAsync(Weapon weapon, CancellationToken ct = default);
    Task DeleteWeaponAsync(int id, CancellationToken ct = default);

    // Actions
    Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default);
    Task<GameAction?> GetActionByIdAsync(int id, CancellationToken ct = default);
    Task AddActionAsync(GameAction action, CancellationToken ct = default);
    Task UpdateActionAsync(GameAction action, CancellationToken ct = default);
    Task DeleteActionAsync(int id, CancellationToken ct = default);

    // Ranks
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);
    Task<Rank?> GetRankByIdAsync(int id, CancellationToken ct = default);
    Task AddRankAsync(Rank rank, CancellationToken ct = default);
    Task UpdateRankAsync(Rank rank, CancellationToken ct = default);
    Task DeleteRankAsync(int id, CancellationToken ct = default);

    // Ribbons
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);
    Task<Ribbon?> GetRibbonByIdAsync(int id, CancellationToken ct = default);
    Task AddRibbonAsync(Ribbon ribbon, CancellationToken ct = default);
    Task UpdateRibbonAsync(Ribbon ribbon, CancellationToken ct = default);
    Task DeleteRibbonAsync(int id, CancellationToken ct = default);

    // Ribbon triggers
    Task<IReadOnlyList<RibbonTrigger>> GetRibbonTriggersAsync(string game, CancellationToken ct = default);
    Task<RibbonTrigger?> GetRibbonTriggerByIdAsync(int id, CancellationToken ct = default);
    Task AddRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default);
    Task UpdateRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default);
    Task DeleteRibbonTriggerAsync(int id, CancellationToken ct = default);

    // Awards
    Task<IReadOnlyList<Award>> GetAwardsAsync(string game, string awardType, CancellationToken ct = default);
    Task<Award?> GetAwardByIdAsync(int id, CancellationToken ct = default);
    Task AddAwardAsync(Award award, CancellationToken ct = default);
    Task UpdateAwardAsync(Award award, CancellationToken ct = default);
    Task DeleteAwardAsync(int id, CancellationToken ct = default);

    // Clan tags
    Task<IReadOnlyList<ClanTag>> GetClanTagsAsync(CancellationToken ct = default);
    Task<ClanTag?> GetClanTagByIdAsync(int id, CancellationToken ct = default);
    Task AddClanTagAsync(ClanTag tag, CancellationToken ct = default);
    Task UpdateClanTagAsync(ClanTag tag, CancellationToken ct = default);
    Task DeleteClanTagAsync(int id, CancellationToken ct = default);

    // Host groups
    Task<IReadOnlyList<HostGroup>> GetHostGroupsAsync(CancellationToken ct = default);
    Task<HostGroup?> GetHostGroupByIdAsync(int id, CancellationToken ct = default);
    Task AddHostGroupAsync(HostGroup group, CancellationToken ct = default);
    Task UpdateHostGroupAsync(HostGroup group, CancellationToken ct = default);
    Task DeleteHostGroupAsync(int id, CancellationToken ct = default);

    // Player edit tools
    Task<Player?> GetPlayerForEditAsync(int playerId, CancellationToken ct = default);
    Task UpdatePlayerAsync(Player player, CancellationToken ct = default);
    Task<IReadOnlyList<(string IpAddress, DateTime LastUsed)>> GetPlayerIpsAsync(int playerId, CancellationToken ct = default);

    // Clan edit tools
    Task<Clan?> GetClanForEditAsync(int clanId, CancellationToken ct = default);
    Task UpdateClanAsync(Clan clan, CancellationToken ct = default);

    // Admin events log
    Task<IReadOnlyList<AdminEvent>> GetAdminEventsAsync(string? eventType, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetAdminEventsCountAsync(string? eventType, CancellationToken ct = default);

    // Tools
    Task OptimizeTablesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> ResetStatsAsync(string? game, ResetOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<string>> CleanupInactiveAsync(string? game, int minKills, CancellationToken ct = default);
    Task<IReadOnlyList<string>> CopyGameSettingsAsync(string fromGame, string toGame, CancellationToken ct = default);
}
