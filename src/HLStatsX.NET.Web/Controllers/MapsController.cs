using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
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

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "kills", CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var result = await _maps.GetAllAsync(game, page, pageSize, sortBy, ct);
        ViewBag.Game = game;
        ViewBag.SortBy = sortBy;
        return View(result);
    }

    public async Task<IActionResult> Detail(string name, string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var map = await _maps.GetByNameAsync(name, game, ct);
        if (map is null) return NotFound();
        ViewBag.Game = game;
        return View(map);
    }
}
