using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface ISearchService
{
    /// <param name="searchType">null/"" = both, "player" = players only, "clan" = clans only</param>
    Task<SearchResults> SearchAsync(string query, string? game, string? searchType = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetVisibleGamesAsync(CancellationToken ct = default);
}
