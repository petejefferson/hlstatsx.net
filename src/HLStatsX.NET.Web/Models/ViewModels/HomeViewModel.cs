using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record HomeViewModel(
    string Game,
    int TotalPlayers,
    int NewPlayersLast24h,       // -1 = no data
    GameStats Stats,
    int ActiveServers,
    IReadOnlyList<Livestat> Livestats,
    IReadOnlyList<Award> DailyAwards,
    IReadOnlyList<ServerLoad> ServerLoad,
    IReadOnlyDictionary<string, Team> Teams
)
{
    public IEnumerable<IGrouping<string, Livestat>> PlayersByTeam =>
        Livestats.GroupBy(l => string.IsNullOrEmpty(l.Team) ? "Unknown" : l.Team);

    public Team? GetTeam(string code) => Teams.TryGetValue(code, out var t) ? t : null;
}
