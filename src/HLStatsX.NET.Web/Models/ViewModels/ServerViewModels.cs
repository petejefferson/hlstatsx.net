using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ServerListViewModel(
    IReadOnlyList<Server> Servers,
    string? Game
);

public record ServerDetailViewModel(
    Server Server,
    IReadOnlyList<Livestat> Livestats
);
