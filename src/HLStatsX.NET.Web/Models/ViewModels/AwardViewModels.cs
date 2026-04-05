using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record AwardListViewModel(
    IReadOnlyList<Award> Awards,
    IReadOnlyList<Award> DailyAwards,
    string Game
);

public record RankListViewModel(
    IReadOnlyList<Rank> Ranks,
    string Game
);

public record RibbonListViewModel(
    IReadOnlyList<Ribbon> Ribbons,
    string Game
);
