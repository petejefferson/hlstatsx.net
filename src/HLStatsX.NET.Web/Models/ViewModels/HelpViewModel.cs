using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record HelpViewModel(
    IReadOnlyList<Weapon> Weapons,
    IReadOnlyList<GameAction> Actions,
    string Mode
);
