using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

/// <summary>A single row in player search results — MatchedName is the alias that matched the query.</summary>
public record PlayerSearchResult(int PlayerId, string MatchedName, string? Flag, string? Country, string Game);

public class SearchResults
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<PlayerSearchResult> Players { get; init; } = Array.Empty<PlayerSearchResult>();
    public IReadOnlyList<Clan> Clans { get; init; } = Array.Empty<Clan>();
    public int TotalPlayers { get; init; }
    public int TotalClans { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
