using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ErrorViewModel(string? RequestId);

public record StatusPageViewModel(int Code, string Title, string Description);

public record HomeViewModel(
    string Game,
    int TotalPlayers,
    int NewPlayersLast24h,       // -1 = no data
    GameStats Stats,
    IReadOnlyList<Server> Servers,
    IReadOnlyList<Livestat> Livestats,
    IReadOnlyList<Award> DailyAwards,
    DateTime? DailyAwardsDate,
    IReadOnlyList<ServerLoad> ServerLoad,
    IReadOnlyDictionary<string, Team> Teams,
    IReadOnlyList<Trend> TrendSeries,
    IReadOnlyList<VoiceCommServer> VoiceCommServers,
    IReadOnlyList<OnlinePlayerLocation> OnlinePlayerLocations,
    bool ShowMap
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
        TrendSeries.Any() ? TrendSeries.Average(t => (double)t.ActSlots) : 0;

    public double AvgPlayersLast1h
    {
        get
        {
            var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
            var recent = TrendSeries.Where(t => t.Timestamp >= cutoff).ToList();
            return recent.Any() ? recent.Average(t => (double)t.ActSlots) : 0;
        }
    }

    /// <summary>
    /// Groups the flat <see cref="ServerLoad"/> list by server, preserving the
    /// display order of <see cref="Servers"/>. Only servers with at least one
    /// load sample are included.
    /// </summary>
    public IReadOnlyList<(Server Server, IReadOnlyList<ServerLoad> Points)> PerServerLoad
    {
        get
        {
            var byServer = ServerLoad
                .GroupBy(s => s.ServerId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ServerLoad>)g.OrderBy(p => p.Timestamp).ToList());
            return Servers
                .Where(s => byServer.ContainsKey(s.ServerId))
                .Select(s => (s, byServer[s.ServerId]))
                .ToList();
        }
    }

    public IEnumerable<IGrouping<string, Livestat>> PlayersByTeam =>
        Livestats
            .GroupBy(l => l.Team ?? "")
            .OrderBy(g => Teams.TryGetValue(g.Key, out var t) ? t.PlayerlistIndex : int.MaxValue);

    public Team? GetTeam(string code) => Teams.TryGetValue(code, out var t) ? t : null;
}
