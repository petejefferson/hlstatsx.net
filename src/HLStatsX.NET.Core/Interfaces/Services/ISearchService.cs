using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for the site-wide search page.
/// Searches player names (all aliases) and clan names simultaneously,
/// returning a combined <see cref="SearchResults"/> object.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches players and/or clans matching <paramref name="query"/>.
    /// </summary>
    /// <param name="searchType">
    /// <c>null</c> or empty = search both players and clans;
    /// <c>"player"</c> = players only; <c>"clan"</c> = clans only.
    /// </param>
    Task<SearchResults> SearchAsync(string query, string? game, string? searchType = null, int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>Returns games that are not hidden, used to populate the search page's game filter dropdown.</summary>
    Task<IReadOnlyList<Game>> GetVisibleGamesAsync(CancellationToken ct = default);
}
