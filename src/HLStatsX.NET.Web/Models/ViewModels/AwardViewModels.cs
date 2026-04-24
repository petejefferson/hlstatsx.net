using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record AwardsIndexViewModel(
    IReadOnlyList<Award> DailyAwards,
    IReadOnlyList<Award> GlobalAwards,
    IReadOnlyList<RankRow> Ranks,
    IReadOnlyList<RibbonRow> Ribbons,
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
