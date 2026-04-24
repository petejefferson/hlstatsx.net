using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class AwardsController : Controller
{
    private readonly IAwardService _awards;
    private readonly IConfiguration _config;

    public AwardsController(IAwardService awards, IConfiguration config)
    {
        _awards = awards;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";

        var dailyTask   = _awards.GetDailyAwardsAsync(game, ct);
        var globalTask  = _awards.GetAwardsAsync(game, ct);
        var ranksTask   = _awards.GetRanksWithCountsAsync(game, ct);
        var ribbonsTask = _awards.GetRibbonsWithCountsAsync(game, ct);

        await Task.WhenAll(dailyTask, globalTask, ranksTask, ribbonsTask);

        return View(new AwardsIndexViewModel(
            dailyTask.Result,
            globalTask.Result,
            ranksTask.Result,
            ribbonsTask.Result,
            game));
    }

    public async Task<IActionResult> RibbonDetail(int id, CancellationToken ct)
    {
        var ribbon = await _awards.GetRibbonAsync(id, ct);
        if (ribbon is null) return NotFound();
        return View(ribbon);
    }

    public async Task<IActionResult> DailyAwardDetail(
        int id, string? game,
        int page = 1, string sortBy = "awardTime", bool desc = true,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var award = await _awards.GetAwardByIdAsync(id, ct);
        if (award is null) return NotFound();

        var history = await _awards.GetDailyAwardHistoryAsync(id, page, pageSize, sortBy, desc, ct);
        return View(new DailyAwardDetailViewModel(award, game, history, sortBy, desc));
    }
}
