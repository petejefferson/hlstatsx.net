using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

public class SearchResults
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<Player> Players { get; init; } = Array.Empty<Player>();
    public IReadOnlyList<Clan> Clans { get; init; } = Array.Empty<Clan>();
    public int TotalPlayers { get; init; }
    public int TotalClans { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
