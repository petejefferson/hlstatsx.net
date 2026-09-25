using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service layer for all player-related operations. Controllers call this interface;
/// implementations delegate to <see cref="Repositories.IPlayerRepository"/> and
/// <see cref="Repositories.IPlayerStatsRepository"/>.
/// </summary>
public interface IPlayerService
{
    /// <summary>Returns the player with the given ID, or <c>null</c> if not found.</summary>
    Task<Player?> GetPlayerAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the paged, sorted leaderboard for the given game, excluding bots and hidden players.</summary>
    Task<PagedResult<Player>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool descending = true, int minKills = 1, CancellationToken ct = default);

    /// <summary>Returns distinct dates for which history snapshots exist, used to populate the leaderboard date picker.</summary>
    Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a leaderboard aggregated from history snapshots for the given date range.</summary>
    Task<PagedResult<PlayerLeaderboardRow>> GetPeriodLeaderboardAsync(string game, DateTime from, DateTime to, int page, int pageSize, string sortBy, bool descending, int minKills = 1, CancellationToken ct = default);

    /// <summary>Returns the player's 1-based rank position within their game.</summary>
    Task<int> GetPlayerRankAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns up to <paramref name="days"/> days of session snapshots for the skill trend chart.</summary>
    Task<IReadOnlyList<PlayerHistory>> GetPlayerHistoryAsync(int playerId, int days = 30, CancellationToken ct = default);

    Task<PagedResult<PlayerEventRow>> GetPlayerEventHistoryAsync(int playerId, string game, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);
    Task<PagedResult<PlayerSessionRow>> GetPlayerSessionsAsync(int playerId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerAward>> GetPlayerAwardsAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<PlayerAwardRow>> GetPlayerAwardsSummaryAsync(int playerId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);
    Task<PagedResult<PlayerAwardRow>> GetPlayerAwardDetailAsync(int playerId, int awardId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerRibbon>> GetPlayerRibbonsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerName>> GetPlayerAliasesAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<PlayerSearchResult>> SearchPlayersAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);
    /// <summary>
    /// Returns a paged, sorted list of banned players for the given game.
    /// Banned players have <c>hideranking = 2</c>. Players with fewer than
    /// <paramref name="minKills"/> kills are excluded.
    /// </summary>
    Task<PagedResult<BanListRow>> GetBannedPlayersAsync(string game, int page, int pageSize, string sortBy, bool desc, int minKills, CancellationToken ct = default);

    /// <summary>
    /// Bans a player by setting <c>HideRanking = 1</c>, removing them from public rankings.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no player with <paramref name="playerId"/> exists.</exception>
    Task BanPlayerAsync(int playerId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Reverses a ban by setting <c>HideRanking = 0</c>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no player with <paramref name="playerId"/> exists.</exception>
    Task UnbanPlayerAsync(int playerId, CancellationToken ct = default);

    Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
    Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default);
    Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default);
    Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default);
    Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default);
    Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default);
    Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default);
    Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default);
    Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, int killLimit = 0, CancellationToken ct = default);
    Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<ServerStatRow>> GetServerPerformanceAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<WeaponStatRow>> GetWeaponStatsAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<WeaponStatsmeRow>> GetWeaponStatsmeAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<WeaponTargetRow>> GetWeaponTargetsAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<TeamStatRow>> GetTeamSelectionAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionVictimsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<TrendPoint>> GetTrendDataAsync(int playerId, int limit = 30, CancellationToken ct = default);
    Task<IReadOnlyList<GlobalAwardRow>> GetGlobalAwardsAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of days after which inactive players are purged,
    /// as configured in the <c>delete_player</c> admin option.
    /// </summary>
    Task<int> GetDeleteDaysAsync(CancellationToken ct = default);

    /// <summary>Returns the tracking mode from <c>hlstats_Options</c> (e.g. <c>"Normal"</c>, <c>"NameTrack"</c>, <c>"LAN"</c>).</summary>
    Task<string> GetModeAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the player ID for an exact uniqueId match within a game, or <c>null</c> if not found.
    /// </summary>
    Task<int?> GetPlayerIdByUniqueIdAsync(string uniqueId, string game, CancellationToken ct = default);
}
