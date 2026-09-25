using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class HelpController : Controller
{
    private readonly IWeaponRepository _weapons;
    private readonly IGameRepository _games;
    private readonly IPlayerService _players;

    public HelpController(IWeaponRepository weapons, IGameRepository games, IPlayerService players)
    {
        _weapons = weapons;
        _games   = games;
        _players = players;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var weaponsTask = _weapons.GetAllForHelpAsync(ct);
        var actionsTask = _games.GetAllActionsAsync(ct);
        var modeTask    = _players.GetModeAsync(ct);
        await Task.WhenAll(weaponsTask, actionsTask, modeTask);

        return View(new HelpViewModel(weaponsTask.Result, actionsTask.Result, modeTask.Result));
    }
}
