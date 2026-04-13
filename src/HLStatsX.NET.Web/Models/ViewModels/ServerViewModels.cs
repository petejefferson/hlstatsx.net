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
    IReadOnlyList<Livestat> Livestats
);
