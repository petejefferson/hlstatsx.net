using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for the admin panel. Covers authentication, site options,
/// game configuration, server management, and bulk maintenance tools.
/// </summary>
/// <remarks>
/// This is the only repository that performs mutations on game-configuration data.
/// Write access in the application is gated behind admin authentication.
/// </remarks>
public interface IAdminRepository
{
    // Admin users

    /// <summary>Returns the admin user record matching the given username, or <c>null</c> if not found.</summary>
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    /// <summary>Returns all admin user records.</summary>
    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Inserts a new admin user.</summary>
    Task AddAsync(AdminUser user, CancellationToken ct = default);
    /// <summary>Persists changes to an existing admin user (e.g. password hash, email).</summary>
    Task UpdateAsync(AdminUser user, CancellationToken ct = default);
    /// <summary>Deletes an admin user by username.</summary>
    Task DeleteAsync(string username, CancellationToken ct = default);

    // Options

    /// <summary>Returns all site-wide options from <c>hlstats_Options</c>.</summary>
    Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default);
    /// <summary>Sets a single option value by its key name.</summary>
    Task SetOptionAsync(string keyName, string value, CancellationToken ct = default);
    /// <summary>Returns the valid choices for a select-style option field.</summary>
    Task<IReadOnlyList<string>> GetOptionChoicesAsync(string keyName, CancellationToken ct = default);

    // Games

    /// <summary>Returns all games registered in the stats daemon's supported games table.</summary>
    Task<IReadOnlyList<GameSupported>> GetSupportedGamesAsync(CancellationToken ct = default);
    /// <summary>Inserts a new game definition.</summary>
    Task AddGameAsync(Game game, CancellationToken ct = default);
    /// <summary>Persists changes to an existing game definition.</summary>
    Task UpdateGameAsync(Game game, CancellationToken ct = default);
    /// <summary>Deletes a game definition by code.</summary>
    Task DeleteGameAsync(string code, CancellationToken ct = default);

    // Servers

    /// <summary>Returns a server by primary key for admin editing, or <c>null</c> if not found.</summary>
    Task<Server?> GetServerByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Inserts a new server.</summary>
    Task AddServerAsync(Server server, CancellationToken ct = default);
    /// <summary>Persists changes to an existing server record.</summary>
    Task UpdateServerAsync(Server server, CancellationToken ct = default);
    /// <summary>Deletes a server and its associated config entries by primary key.</summary>
    Task DeleteServerAsync(int id, CancellationToken ct = default);
    /// <summary>Returns all per-server config overrides for the given server.</summary>
    Task<IReadOnlyList<ServerConfig>> GetServerConfigAsync(int serverId, CancellationToken ct = default);
    /// <summary>Returns the default config values for the game (applied when a server has no override).</summary>
    Task<IReadOnlyList<ServerConfig>> GetServerConfigDefaultsAsync(string game, CancellationToken ct = default);
    /// <summary>Sets a single config parameter override for a server.</summary>
    Task SetServerConfigAsync(int serverId, string parameter, string value, CancellationToken ct = default);
    /// <summary>Copies all config entries from one server to another.</summary>
    Task CopyServerConfigAsync(int fromServerId, int toServerId, CancellationToken ct = default);
    /// <summary>Resets a server's config overrides back to game defaults.</summary>
    Task ResetServerConfigToDefaultsAsync(int serverId, string game, CancellationToken ct = default);

    // Teams

    /// <summary>Returns all teams for the given game.</summary>
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single team by primary key, or <c>null</c> if not found.</summary>
    Task<Team?> GetTeamByIdAsync(int id, CancellationToken ct = default);
    Task AddTeamAsync(Team team, CancellationToken ct = default);
    Task UpdateTeamAsync(Team team, CancellationToken ct = default);
    Task DeleteTeamAsync(int id, CancellationToken ct = default);

    // Roles

    /// <summary>Returns all player roles/classes for the given game.</summary>
    Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single role by primary key, or <c>null</c> if not found.</summary>
    Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default);
    Task AddRoleAsync(Role role, CancellationToken ct = default);
    Task UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task DeleteRoleAsync(int id, CancellationToken ct = default);

    // Weapons

    /// <summary>Returns all weapon definitions for the given game.</summary>
    Task<IReadOnlyList<Weapon>> GetWeaponsAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single weapon by primary key, or <c>null</c> if not found.</summary>
    Task<Weapon?> GetWeaponByIdAsync(int id, CancellationToken ct = default);
    Task AddWeaponAsync(Weapon weapon, CancellationToken ct = default);
    Task UpdateWeaponAsync(Weapon weapon, CancellationToken ct = default);
    Task DeleteWeaponAsync(int id, CancellationToken ct = default);

    // Actions

    /// <summary>Returns all game-action definitions for the given game.</summary>
    Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single action by primary key, or <c>null</c> if not found.</summary>
    Task<GameAction?> GetActionByIdAsync(int id, CancellationToken ct = default);
    Task AddActionAsync(GameAction action, CancellationToken ct = default);
    Task UpdateActionAsync(GameAction action, CancellationToken ct = default);
    Task DeleteActionAsync(int id, CancellationToken ct = default);

    // Ranks

    /// <summary>Returns all rank tiers for the given game, ordered by kill threshold.</summary>
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single rank by primary key, or <c>null</c> if not found.</summary>
    Task<Rank?> GetRankByIdAsync(int id, CancellationToken ct = default);
    Task AddRankAsync(Rank rank, CancellationToken ct = default);
    Task UpdateRankAsync(Rank rank, CancellationToken ct = default);
    Task DeleteRankAsync(int id, CancellationToken ct = default);

    // Ribbons

    /// <summary>Returns all ribbons defined for the given game.</summary>
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single ribbon by primary key, or <c>null</c> if not found.</summary>
    Task<Ribbon?> GetRibbonByIdAsync(int id, CancellationToken ct = default);
    Task AddRibbonAsync(Ribbon ribbon, CancellationToken ct = default);
    Task UpdateRibbonAsync(Ribbon ribbon, CancellationToken ct = default);
    Task DeleteRibbonAsync(int id, CancellationToken ct = default);

    // Ribbon triggers

    /// <summary>Returns the award-based trigger conditions that unlock ribbons for the given game.</summary>
    Task<IReadOnlyList<RibbonTrigger>> GetRibbonTriggersAsync(string game, CancellationToken ct = default);
    /// <summary>Returns a single ribbon trigger by primary key, or <c>null</c> if not found.</summary>
    Task<RibbonTrigger?> GetRibbonTriggerByIdAsync(int id, CancellationToken ct = default);
    Task AddRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default);
    Task UpdateRibbonTriggerAsync(RibbonTrigger trigger, CancellationToken ct = default);
    Task DeleteRibbonTriggerAsync(int id, CancellationToken ct = default);

    // Awards

    /// <summary>Returns all awards of the given type (<c>"W"</c> = weapon, <c>"E"</c> = event, <c>"D"</c> = daily) for the game.</summary>
    Task<IReadOnlyList<Award>> GetAwardsAsync(string game, string awardType, CancellationToken ct = default);
    /// <summary>Returns a single award by primary key, or <c>null</c> if not found.</summary>
    Task<Award?> GetAwardByIdAsync(int id, CancellationToken ct = default);
    Task AddAwardAsync(Award award, CancellationToken ct = default);
    Task UpdateAwardAsync(Award award, CancellationToken ct = default);
    Task DeleteAwardAsync(int id, CancellationToken ct = default);

    // Clan tags

    /// <summary>Returns all configured clan-tag patterns used to auto-assign players to clans.</summary>
    Task<IReadOnlyList<ClanTag>> GetClanTagsAsync(CancellationToken ct = default);
    /// <summary>Returns a single clan-tag pattern by primary key, or <c>null</c> if not found.</summary>
    Task<ClanTag?> GetClanTagByIdAsync(int id, CancellationToken ct = default);
    Task AddClanTagAsync(ClanTag tag, CancellationToken ct = default);
    Task UpdateClanTagAsync(ClanTag tag, CancellationToken ct = default);
    Task DeleteClanTagAsync(int id, CancellationToken ct = default);

    // Host groups

    /// <summary>Returns all host groups used to exclude known bots or LAN ranges from rankings.</summary>
    Task<IReadOnlyList<HostGroup>> GetHostGroupsAsync(CancellationToken ct = default);
    /// <summary>Returns a single host group by primary key, or <c>null</c> if not found.</summary>
    Task<HostGroup?> GetHostGroupByIdAsync(int id, CancellationToken ct = default);
    Task AddHostGroupAsync(HostGroup group, CancellationToken ct = default);
    Task UpdateHostGroupAsync(HostGroup group, CancellationToken ct = default);
    Task DeleteHostGroupAsync(int id, CancellationToken ct = default);

    // Player edit tools

    /// <summary>Returns a player record loaded for admin editing (name, country, hide flags).</summary>
    Task<Player?> GetPlayerForEditAsync(int playerId, CancellationToken ct = default);
    Task UpdatePlayerAsync(Player player, CancellationToken ct = default);
    /// <summary>Returns the IP addresses a player has connected from, most recent first.</summary>
    Task<IReadOnlyList<(string IpAddress, DateTime LastUsed)>> GetPlayerIpsAsync(int playerId, CancellationToken ct = default);

    // Clan edit tools

    /// <summary>Returns a clan record loaded for admin editing.</summary>
    Task<Clan?> GetClanForEditAsync(int clanId, CancellationToken ct = default);
    Task UpdateClanAsync(Clan clan, CancellationToken ct = default);

    // Admin events log

    /// <summary>Returns the admin audit log, optionally filtered by event type and paged.</summary>
    Task<IReadOnlyList<AdminEvent>> GetAdminEventsAsync(string? eventType, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetAdminEventsCountAsync(string? eventType, CancellationToken ct = default);

    // Tools

    /// <summary>Runs <c>OPTIMIZE TABLE</c> on all HLStatsX tables to reclaim fragmented space.</summary>
    Task OptimizeTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Resets player statistics according to the given <paramref name="options"/> (kills, sessions, etc.).
    /// Returns a list of log messages describing each step taken.
    /// </summary>
    Task<IReadOnlyList<string>> ResetStatsAsync(string? game, ResetOptions options, CancellationToken ct = default);

    /// <summary>
    /// Deletes player records that have fewer than <paramref name="minKills"/> career kills.
    /// Returns a list of log messages describing which records were removed.
    /// </summary>
    Task<IReadOnlyList<string>> CleanupInactiveAsync(string? game, int minKills, CancellationToken ct = default);

    /// <summary>Copies weapons, actions, teams, and roles from one game to another.</summary>
    Task<IReadOnlyList<string>> CopyGameSettingsAsync(string fromGame, string toGame, CancellationToken ct = default);

    // Voice comm servers

    Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default);
    Task<VoiceCommServer?> GetVoiceCommServerByIdAsync(int id, CancellationToken ct = default);
    Task AddVoiceCommServerAsync(VoiceCommServer server, CancellationToken ct = default);
    Task UpdateVoiceCommServerAsync(VoiceCommServer server, CancellationToken ct = default);
    Task DeleteVoiceCommServerAsync(int id, CancellationToken ct = default);

    // DB collations

    /// <summary>
    /// Converts all tables and columns to utf8mb4/utf8mb4_unicode_ci.
    /// In <paramref name="printOnly"/> mode returns SQL statements instead of executing them.
    /// </summary>
    Task<IReadOnlyList<string>> ResetCollationsAsync(bool printOnly, CancellationToken ct = default);

    // IP stats

    /// <summary>
    /// Lists connection frequency grouped by hostgroup (ISP / organisation), paginated.
    /// Empty hostgroup values are converted to <c>"(Unresolved IP Addresses)"</c>.
    /// Percentage is calculated against the total row count across all groups.
    /// </summary>
    Task<PagedResult<IpStatsHostGroupRow>> GetIpStatsByHostGroupAsync(int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>
    /// Lists connection frequency for individual hostnames / IP addresses within a single
    /// <paramref name="hostGroup"/>. Pass <c>"(Unresolved IP Addresses)"</c> to query
    /// events with an empty hostgroup. Percentage is relative to the group total.
    /// </summary>
    Task<PagedResult<IpStatsHostRow>> GetIpStatsByHostAsync(string hostGroup, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
}
