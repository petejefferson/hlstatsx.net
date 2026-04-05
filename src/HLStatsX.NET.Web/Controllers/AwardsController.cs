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
        var awards = await _awards.GetAwardsAsync(game, ct);
        var dailyAwards = await _awards.GetDailyAwardsAsync(game, ct);
        return View(new AwardListViewModel(awards, dailyAwards, game));
    }

    public async Task<IActionResult> Ranks(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var ranks = await _awards.GetRanksAsync(game, ct);
        return View(new RankListViewModel(ranks, game));
    }

    public async Task<IActionResult> Ribbons(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var ribbons = await _awards.GetRibbonsAsync(game, ct);
        return View(new RibbonListViewModel(ribbons, game));
    }

    public async Task<IActionResult> RibbonDetail(int id, CancellationToken ct)
    {
        var ribbon = await _awards.GetRibbonAsync(id, ct);
        if (ribbon is null) return NotFound();
        return View(ribbon);
    }
}
