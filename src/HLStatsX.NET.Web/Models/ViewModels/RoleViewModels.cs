using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record RoleListViewModel(
    IReadOnlyList<Role> Roles,
    string Game,
    string SortBy,
    bool Descending,
    int TotalKills,
    int TotalDeaths,
    int TotalPicked
);

public record RoleDetailViewModel(
    Role Role,
    string Game,
    PagedResult<RoleKillerRow> Killers,
    int TotalKills,
    int TotalHeadshots,
    string SortBy,
    bool Descending
);
