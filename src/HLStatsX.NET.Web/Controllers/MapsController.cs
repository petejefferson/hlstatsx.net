using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class MapsController : Controller
{
    private readonly IMapRepository _maps;
    private readonly IConfiguration _config;

    public MapsController(IMapRepository maps, IConfiguration config)
    {
        _maps = maps;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var resultTask = _maps.GetAllAsync(game, page, pageSize, sortBy, desc, ct);
        var totalsTask = _maps.GetKillTotalsAsync(game, ct);
        await Task.WhenAll(resultTask, totalsTask);
        var totals = totalsTask.Result;
        return View(new MapListViewModel(resultTask.Result, game, sortBy, desc, totals.TotalKills, totals.TotalHeadshots));
    }

    public async Task<IActionResult> Detail(string name, string? game, int page = 1, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var totalKillsTask = _maps.GetMapTotalKillsAsync(name, game, ct);
        var playersTask    = _maps.GetPlayerLeaderboardAsync(name, game, page, pageSize, sortBy, desc, ct);
        await Task.WhenAll(totalKillsTask, playersTask);

        return View(new MapDetailViewModel(name, game, totalKillsTask.Result, playersTask.Result, sortBy, desc));
    }
}
