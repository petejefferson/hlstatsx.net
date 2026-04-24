using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class ActionsController : Controller
{
    private readonly IActionRepository _actions;
    private readonly IConfiguration _config;

    public ActionsController(IActionRepository actions, IConfiguration config)
    {
        _actions = actions;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, string sortBy = "count", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var listTask = _actions.GetListAsync(game, sortBy, desc, ct);
        var totalTask = _actions.GetTotalEarnedAsync(game, ct);
        await Task.WhenAll(listTask, totalTask);
        return View(new ActionListViewModel(listTask.Result, game, sortBy, desc, totalTask.Result));
    }

    public async Task<IActionResult> Detail(
        string code, string? game,
        int page = 1, string sortBy = "count", bool desc = true,
        int vpage = 1, string vsortBy = "count", bool vdesc = true,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        const int pageSize = 40;

        var action = await _actions.GetByCodeAsync(code, game, ct);
        if (action is null) return NotFound();

        bool usePlayerPlayerActions = action.ForPlayerPlayerActions;

        var achieverTask = _actions.GetAchieversAsync(code, game, usePlayerPlayerActions, page, pageSize, sortBy, desc, ct);
        var totalTask = _actions.GetTotalAchievementsAsync(code, game, usePlayerPlayerActions, ct);
        await Task.WhenAll(achieverTask, totalTask);

        PagedResult<ActionVictimRow>? victims = null;
        if (action.ForPlayerPlayerActions)
        {
            victims = await _actions.GetVictimsAsync(code, game, vpage, pageSize, vsortBy, vdesc, ct);
        }

        return View(new ActionDetailViewModel(
            action, game,
            achieverTask.Result, totalTask.Result,
            sortBy, desc,
            victims, vsortBy, vdesc));
    }
}
