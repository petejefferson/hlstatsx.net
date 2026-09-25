using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record AwardsIndexViewModel(
    IReadOnlyList<Award> DailyAwards,
    IReadOnlyList<Award> GlobalAwards,
    IReadOnlyList<RankRow> Ranks,
    IReadOnlyList<RibbonRow> Ribbons,
    string Game,
    DateTime? DailyAwardsDate,
    int AwardsNumDays
);

public record RankListViewModel(
    IReadOnlyList<Rank> Ranks,
    string Game
);

public record RibbonListViewModel(
    IReadOnlyList<Ribbon> Ribbons,
    string Game
);

public record DailyAwardDetailViewModel(
    Award Award,
    string Game,
    PagedResult<DailyAwardHistoryRow> History,
    string SortBy,
    bool Descending
);

public record RankDetailViewModel(
    Rank Rank,
    string Game,
    PagedResult<RankPlayerRow> Players,
    string SortBy,
    bool Descending
);

public record RibbonDetailViewModel(
    Ribbon Ribbon,
    string Game,
    PagedResult<RibbonDetailRow> Players,
    string SortBy,
    bool Descending
);
