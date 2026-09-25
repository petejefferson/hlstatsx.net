using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

public record PlayerSearchResult(int PlayerId, string MatchedName, string? Flag, string? Country, string Game, bool IsBot = false);
public record UniqueIdSearchResult(int PlayerId, string UniqueId, string LastName, string? Flag, string? Country, string GameName);

public class SearchResults
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<PlayerSearchResult> Players { get; init; } = Array.Empty<PlayerSearchResult>();
    public IReadOnlyList<Clan> Clans { get; init; } = Array.Empty<Clan>();
    public IReadOnlyList<UniqueIdSearchResult> UniqueIds { get; init; } = Array.Empty<UniqueIdSearchResult>();
    public int TotalPlayers { get; init; }
    public int TotalClans { get; init; }
    public int TotalUniqueIds { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
