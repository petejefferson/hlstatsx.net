using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for the admin panel. Covers authentication, site options,
/// game/server/weapon/role configuration, and bulk maintenance tools.
/// Mirrors <see cref="IAdminRepository"/> but adds password hashing for user management
/// and composite save operations for multi-field forms.
/// </summary>
public interface IAdminService
{
    // Auth

    /// <summary>
    /// Authenticates an admin user by username and password.
    /// Returns the user record on success, or <c>null</c> if credentials are invalid.
    /// </summary>
    Task<AdminUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default);

    // Users

    /// <summary>Returns all admin user records.</summary>
    Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default);
    /// <summary>Creates a new admin user, hashing <paramref name="password"/> before storing it.</summary>
    Task CreateUserAsync(AdminUser user, string password, CancellationToken ct = default);
    /// <summary>Updates an admin user; if <paramref name="newPassword"/> is non-null it is hashed and saved.</summary>
    Task UpdateUserAsync(AdminUser user, string? newPassword, CancellationToken ct = default);
    /// <summary>Deletes an admin user by username.</summary>
    Task DeleteUserAsync(string username, CancellationToken ct = default);

    // Options

    /// <summary>Returns all site-wide options as a key-value dictionary.</summary>
    Task<Dictionary<string, string>> GetOptionsAsync(CancellationToken ct = default);
    /// <summary>Sets a single option value by key name.</summary>
    Task SetOptionAsync(string keyName, string value, CancellationToken ct = default);
    /// <summary>Saves multiple option values in one call (used by the options form submit).</summary>
    Task SaveOptionsAsync(Dictionary<string, string> values, CancellationToken ct = default);

    // Games

    /// <summary>Returns all games registered in the stats daemon's supported-games table.</summary>
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

    /// <summary>Returns awards of the given type for the game (<c>"W"</c>, <c>"E"</c>, or <c>"D"</c>).</summary>
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

    // Player / clan tools

    Task<Player?> GetPlayerForEditAsync(int playerId, CancellationToken ct = default);
    Task UpdatePlayerAsync(Player player, CancellationToken ct = default);
    Task<IReadOnlyList<(string IpAddress, DateTime LastUsed)>> GetPlayerIpsAsync(int playerId, CancellationToken ct = default);
    Task<Clan?> GetClanForEditAsync(int clanId, CancellationToken ct = default);
    Task UpdateClanAsync(Clan clan, CancellationToken ct = default);

    // Admin events

    /// <summary>Returns the admin audit log, optionally filtered by event type and paged.</summary>
    Task<IReadOnlyList<AdminEvent>> GetAdminEventsAsync(string? eventType, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetAdminEventsCountAsync(string? eventType, CancellationToken ct = default);

    // Tools

    /// <summary>Runs <c>OPTIMIZE TABLE</c> on all HLStatsX tables.</summary>
    Task OptimizeTablesAsync(CancellationToken ct = default);

    /// <summary>Resets player statistics per the given options. Returns log messages for each step.</summary>
    Task<IReadOnlyList<string>> ResetStatsAsync(string? game, ResetOptions options, CancellationToken ct = default);

    /// <summary>Deletes players with fewer than <paramref name="minKills"/> career kills. Returns log messages.</summary>
    Task<IReadOnlyList<string>> CleanupInactiveAsync(string? game, int minKills, CancellationToken ct = default);

    /// <summary>Copies weapons, actions, teams, and roles from one game to another. Returns log messages.</summary>
    Task<IReadOnlyList<string>> CopyGameSettingsAsync(string fromGame, string toGame, CancellationToken ct = default);

    // Voice comm servers

    Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default);
    Task<VoiceCommServer?> GetVoiceCommServerByIdAsync(int id, CancellationToken ct = default);
    Task AddVoiceCommServerAsync(VoiceCommServer server, CancellationToken ct = default);
    Task UpdateVoiceCommServerAsync(VoiceCommServer server, CancellationToken ct = default);
    Task DeleteVoiceCommServerAsync(int id, CancellationToken ct = default);

    // DB collations

    Task<IReadOnlyList<string>> ResetCollationsAsync(bool printOnly, CancellationToken ct = default);

    // ── IP stats ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists connection frequency grouped by hostgroup (ISP / organisation), paginated.
    /// Empty hostgroup values are converted to <c>"(Unresolved IP Addresses)"</c>.
    /// Percentage is calculated against total connections across all groups.
    /// </summary>
    Task<PagedResult<IpStatsHostGroupRow>> GetIpStatsByHostGroupAsync(int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>
    /// Lists connection frequency for individual hostnames / IP addresses within a single
    /// <paramref name="hostGroup"/>. Pass <c>"(Unresolved IP Addresses)"</c> to query
    /// events with an empty hostgroup. Percentage is relative to the group total.
    /// </summary>
    Task<PagedResult<IpStatsHostRow>> GetIpStatsByHostAsync(string hostGroup, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
}
