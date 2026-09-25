using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Core player data access: CRUD, leaderboard, search, history, and basic profile lists.
/// Profile-stat queries (kills breakdown, weapon usage, etc.) live in IPlayerStatsRepository.
/// </summary>
public interface IPlayerRepository
{
    /// <summary>Returns the player with the given ID including their clan and Steam IDs, or <c>null</c> if not found.</summary>
    Task<Player?> GetByIdAsync(int playerId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged, sorted leaderboard for the given game.
    /// Hidden players (<c>HideRanking != 0</c>) and players with fewer than <paramref name="minKills"/>
    /// kills are excluded.
    /// </summary>
    Task<PagedResult<Player>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool descending = true, int minKills = 1, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent distinct snapshot dates available in <c>hlstats_Players_History</c>,
    /// used to populate the history date picker on the leaderboard.
    /// </summary>
    Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Returns a leaderboard aggregated from session history snapshots between
    /// <paramref name="from"/> and <paramref name="to"/> (useful for weekly/monthly rankings).
    /// </summary>
    Task<PagedResult<PlayerLeaderboardRow>> GetHistoryRankingsAsync(string game, DateTime from, DateTime to, int page, int pageSize, string sortBy = "kills", bool descending = true, int minKills = 1, CancellationToken ct = default);

    /// <summary>Returns all name aliases the player has used, most recently used first.</summary>
    Task<IReadOnlyList<PlayerName>> GetAliasesAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns up to <paramref name="days"/> days of session history snapshots for the trend chart.</summary>
    Task<IReadOnlyList<PlayerHistory>> GetHistoryAsync(int playerId, int days = 30, CancellationToken ct = default);

    /// <summary>Returns a paged, sorted event log (kills, deaths, connects, etc.) for the player.</summary>
    Task<PagedResult<PlayerEventRow>> GetEventHistoryAsync(int playerId, string game, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);

    /// <summary>Returns a paged, sorted list of session rows from <c>hlstats_Players_History</c>.</summary>
    Task<PagedResult<PlayerSessionRow>> GetSessionsAsync(int playerId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);

    /// <summary>Returns all awards ever won by the player.</summary>
    Task<IReadOnlyList<PlayerAward>> GetAwardsAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns a paged summary of awards grouped by award type.</summary>
    Task<PagedResult<PlayerAwardRow>> GetAwardsSummaryAsync(int playerId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);

    /// <summary>Returns a paged list of every time the player won a specific award.</summary>
    Task<PagedResult<PlayerAwardRow>> GetAwardDetailAsync(int playerId, int awardId, int page, int pageSize, string sortBy, bool descending, CancellationToken ct = default);

    /// <summary>Returns all ribbons the player has earned.</summary>
    Task<IReadOnlyList<PlayerRibbon>> GetRibbonsAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the player's 1-based rank position in the game leaderboard.</summary>
    Task<int> GetRankAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Searches <c>hlstats_PlayerNames</c> for players whose aliases match the query.</summary>
    Task<PagedResult<PlayerSearchResult>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Searches players by Steam ID / unique identifier.</summary>
    Task<PagedResult<UniqueIdSearchResult>> SearchByUniqueIdAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged, sorted list of banned players (<c>hideranking = 2</c>) for the given game.
    /// Players with fewer than <paramref name="minKills"/> kills are excluded.
    /// </summary>
    Task<PagedResult<BanListRow>> GetBannedAsync(string game, int page, int pageSize, string sortBy, bool desc, int minKills, CancellationToken ct = default);

    /// <summary>Looks up a player by their Steam ID string (e.g. <c>"76561197960265728"</c>) within a game.</summary>
    Task<Player?> GetBySteamIdAsync(string steamId, string game, CancellationToken ct = default);

    /// <summary>Persists changes to an existing player row.</summary>
    Task UpdateAsync(Player player, CancellationToken ct = default);

    /// <summary>Returns the total number of non-hidden players for the given game.</summary>
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);

    /// <summary>Returns the sum of all kills across all players for the given game.</summary>
    Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns up to <paramref name="limit"/> skill trend data points for the player, newest first.</summary>
    Task<IReadOnlyList<TrendPoint>> GetTrendAsync(int playerId, int limit = 30, CancellationToken ct = default);

    /// <summary>
    /// Returns the player ID for an exact uniqueId match within a game.
    /// Returns <c>null</c> when no match or multiple matches exist.
    /// </summary>
    Task<int?> GetPlayerIdByUniqueIdAsync(string uniqueId, string game, CancellationToken ct = default);
}
