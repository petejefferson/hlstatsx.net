using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for map play statistics stored in <c>hlstats_Events_Frags</c>
/// and the <c>hlstats_Maps</c> counter table.
/// </summary>
public interface IMapRepository
{
    /// <summary>Returns the map record for a specific map name within a game, or <c>null</c> if it has never been played.</summary>
    Task<MapCount?> GetByNameAsync(string mapName, string game, CancellationToken ct = default);

    /// <summary>Returns a paged, sortable list of all maps that have been played on this game.</summary>
    Task<PagedResult<MapCount>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default);

    /// <summary>Returns the top <paramref name="count"/> maps by kill count (used for home page highlights).</summary>
    Task<IReadOnlyList<MapCount>> GetTopMapsAsync(string game, int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Returns total kills and headshots across all maps for the game.
    /// Used to calculate per-map percentage bars on the leaderboard.
    /// </summary>
    Task<(long TotalKills, long TotalHeadshots)> GetKillTotalsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a paged leaderboard of players sorted by kill count on a specific map.</summary>
    Task<PagedResult<MapPlayerRow>> GetPlayerLeaderboardAsync(string map, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns total kills on a specific map (used as the denominator for per-player share bars).</summary>
    Task<long> GetMapTotalKillsAsync(string map, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the map download URL from the <c>map_dlurl</c> option with <c>%MAP%</c> and <c>%GAME%</c>
    /// substituted, or <c>null</c> if the option is empty or not set.
    /// </summary>
    Task<string?> GetMapDownloadUrlAsync(string mapName, string game, CancellationToken ct = default);
}
