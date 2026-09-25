using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

/// <summary>
/// Handles the awards index (daily and global awards, ranks, ribbons),
/// ribbon detail, daily award history, and rank detail pages.
/// </summary>
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
        var dateTask    = _awards.GetAwardDateInfoAsync(ct);

        await Task.WhenAll(dailyTask, globalTask, ranksTask, ribbonsTask, dateTask);
        var (awardDate, numDays) = dateTask.Result;

        return View(new AwardsIndexViewModel(
            dailyTask.Result,
            globalTask.Result,
            ranksTask.Result,
            ribbonsTask.Result,
            game,
            awardDate,
            numDays));
    }

    public async Task<IActionResult> RibbonDetail(
        int id, string? game,
        int page = 1, string sortBy = "numawards", bool desc = true,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var ribbon = await _awards.GetRibbonAsync(id, ct);
        if (ribbon is null) return NotFound();

        var players = await _awards.GetRibbonDetailAsync(id, game, page, pageSize, sortBy, desc, ct);
        return View(new RibbonDetailViewModel(ribbon, game, players, sortBy, desc));
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

    public async Task<IActionResult> RankDetail(
        int rank, string? game,
        int page = 1, string sortBy = "skill", bool desc = true,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var rankEntity = await _awards.GetRankByIdAsync(rank, ct);
        if (rankEntity is null) return NotFound();

        var players = await _awards.GetRankDetailAsync(rank, game, page, pageSize, sortBy, desc, ct);
        return View(new RankDetailViewModel(rankEntity, game, players, sortBy, desc));
    }
}
