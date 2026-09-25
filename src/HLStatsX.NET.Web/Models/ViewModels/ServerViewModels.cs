using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ServerListViewModel(
    IReadOnlyList<Server> Servers,
    string? Game,
    int TotalPlayers,
    int NewPlayersLast24h,   // -1 = no trend data
    GameStats Stats,
    IReadOnlyList<Trend> TrendSeries
);

public record ServerDetailViewModel(
    Server Server,
    string Game,
    int TotalPlayers,
    int NewPlayersLast24h,       // -1 = no data
    GameStats Stats,
    IReadOnlyList<Livestat> Livestats,
    IReadOnlyList<Award> DailyAwards,
    DateTime? DailyAwardsDate,
    IReadOnlyList<ServerLoad> ServerLoad,
    IReadOnlyDictionary<string, Team> Teams,
    IReadOnlyList<Trend> TrendSeries
)
{
    public int NewPlayersLast1h
    {
        get
        {
            var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
            var snapshot = TrendSeries.LastOrDefault(t => t.Timestamp <= cutoff);
            return snapshot is not null ? TotalPlayers - snapshot.Players : -1;
        }
    }

    public double AvgPlayersLast24h =>
        ServerLoad.Any() ? ServerLoad.Average(s => (double)s.ActPlayers) : 0;

    public double AvgPlayersLast1h
    {
        get
        {
            var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
            var recent = ServerLoad.Where(s => s.Timestamp >= cutoff).ToList();
            return recent.Any() ? recent.Average(s => (double)s.ActPlayers) : 0;
        }
    }

    public IEnumerable<IGrouping<string, Livestat>> PlayersByTeam =>
        Livestats
            .GroupBy(l => l.Team ?? "")
            .OrderBy(g => Teams.TryGetValue(g.Key, out var t) ? t.PlayerlistIndex : int.MaxValue);

    public Team? GetTeam(string code) => Teams.TryGetValue(code, out var t) ? t : null;
}

public record ServerLivestatsViewModel(
    Server Server,
    string Game,
    IReadOnlyList<Livestat> Livestats,
    IReadOnlyDictionary<string, Team> Teams
)
{
    public IEnumerable<IGrouping<string, Livestat>> PlayersByTeam =>
        Livestats
            .GroupBy(l => l.Team ?? "")
            .OrderBy(g => Teams.TryGetValue(g.Key, out var t) ? t.PlayerlistIndex : int.MaxValue);

    public Team? GetTeam(string code) => Teams.TryGetValue(code, out var t) ? t : null;
}
