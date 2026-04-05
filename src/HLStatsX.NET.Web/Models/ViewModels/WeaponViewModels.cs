using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record WeaponListViewModel(
    PagedResult<Weapon> Weapons,
    string Game,
    string SortBy
);

public record WeaponDetailViewModel(
    Weapon Weapon,
    string Game
);
