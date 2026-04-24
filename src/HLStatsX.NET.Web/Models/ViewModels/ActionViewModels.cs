using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ActionListViewModel(
    IReadOnlyList<ActionListRow> Actions,
    string Game,
    string SortBy,
    bool Descending,
    long TotalEarned
);

public record ActionDetailViewModel(
    GameAction Action,
    string Game,
    PagedResult<ActionAchieverRow> Achievers,
    long TotalAchievements,
    string SortBy,
    bool Descending,
    PagedResult<ActionVictimRow>? Victims,
    string VSortBy,
    bool VDescending
);
