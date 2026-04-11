using HLStatsX.NET.Core.Entities;
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

    public async Task<SearchResults> SearchAsync(string query, string? game, string? searchType = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var wantPlayers = string.IsNullOrEmpty(searchType) || searchType == "player";
        var wantClans   = string.IsNullOrEmpty(searchType) || searchType == "clan";

        var playerResult = wantPlayers
            ? await _players.SearchAsync(query, game, page, pageSize, ct)
            : PagedResult<PlayerSearchResult>.Create([], 0, page, pageSize);

        var clanResult = wantClans
            ? await _clans.SearchAsync(query, game, page, pageSize, ct)
            : PagedResult<Clan>.Create([], 0, page, pageSize);

        return new SearchResults
        {
            Query        = query,
            Players      = playerResult.Items,
            Clans        = clanResult.Items,
            TotalPlayers = playerResult.TotalCount,
            TotalClans   = clanResult.TotalCount,
            Page         = page,
            PageSize     = pageSize
        };
    }
}
