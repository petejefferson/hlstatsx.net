using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IPlayerRepository _players;
    private readonly IClanRepository _clans;

    public SearchService(IPlayerRepository players, IClanRepository clans)
    {
        _players = players;
        _clans = clans;
    }

    public async Task<SearchResults> SearchAsync(string query, string? game, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var players = await _players.SearchAsync(query, game, page, pageSize, ct);
        var clans = await _clans.SearchAsync(query, game, page, pageSize, ct);

        return new SearchResults
        {
            Query    = query,
            Players  = players.Items,
            Clans    = clans.Items,
            TotalPlayers = players.TotalCount,
            TotalClans   = clans.TotalCount,
            Page     = page,
            PageSize = pageSize
        };
    }
}
