using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IPlayerRepository _players;
    private readonly IClanRepository _clans;
    private readonly IGameRepository _games;

    public SearchService(IPlayerRepository players, IClanRepository clans, IGameRepository games)
    {
        _players = players;
        _clans = clans;
        _games = games;
    }

    public async Task<SearchResults> SearchAsync(string query, string? game, string? searchType = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var wantPlayers  = string.IsNullOrEmpty(searchType) || searchType == "player";
        var wantClans    = string.IsNullOrEmpty(searchType) || searchType == "clan";
        var wantUniqueId = string.IsNullOrEmpty(searchType) || searchType == "uniqueid";

        var playerTask   = wantPlayers
            ? _players.SearchAsync(query, game, page, pageSize, ct)
            : Task.FromResult(PagedResult<PlayerSearchResult>.Create([], 0, page, pageSize));

        var clanTask     = wantClans
            ? _clans.SearchAsync(query, game, page, pageSize, ct)
            : Task.FromResult(PagedResult<Clan>.Create([], 0, page, pageSize));

        var uniqueIdTask = wantUniqueId
            ? _players.SearchByUniqueIdAsync(query, game, page, pageSize, ct)
            : Task.FromResult(PagedResult<UniqueIdSearchResult>.Create([], 0, page, pageSize));

        await Task.WhenAll(playerTask, clanTask, uniqueIdTask);

        return new SearchResults
        {
            Query          = query,
            Players        = playerTask.Result.Items,
            Clans          = clanTask.Result.Items,
            UniqueIds      = uniqueIdTask.Result.Items,
            TotalPlayers   = playerTask.Result.TotalCount,
            TotalClans     = clanTask.Result.TotalCount,
            TotalUniqueIds = uniqueIdTask.Result.TotalCount,
            Page           = page,
            PageSize       = pageSize
        };
    }

    public async Task<IReadOnlyList<Game>> GetVisibleGamesAsync(CancellationToken ct = default)
    {
        var all = await _games.GetAllAsync(ct);
        return all.Where(g => !g.IsHidden).OrderBy(g => g.Name).ToList();
    }
}
