using HLStatsX.NET.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class SearchController : Controller
{
    private readonly ISearchService _search;
    private readonly IConfiguration _config;

    public SearchController(ISearchService search, IConfiguration config)
    {
        _search = search;
        _config = config;
    }

    public async Task<IActionResult> Index(string? q, string? game, string? st = null, int page = 1, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        ViewData["game"]  = game;
        ViewData["query"] = q ?? "";
        ViewData["st"]    = st ?? "";

        if (string.IsNullOrWhiteSpace(q))
        {
            ViewData["HideBotPlayers"] = _config.GetValue<bool>("HLStatsX:HideBotPlayers", true);
            return View("Index", (object?)null);
        }

        ViewData["HideBotPlayers"] = _config.GetValue<bool>("HLStatsX:HideBotPlayers", true);
        var results = await _search.SearchAsync(q, game, st, page, 50, ct);
        return View(results);
    }
}
