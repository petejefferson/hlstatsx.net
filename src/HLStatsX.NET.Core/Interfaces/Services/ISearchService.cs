using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, string? game, int page = 1, int pageSize = 20, CancellationToken ct = default);
}
