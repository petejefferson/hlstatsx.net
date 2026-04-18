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
        game ??= "";
        ViewData["game"]  = game;
        ViewData["query"] = q ?? "";
        ViewData["st"]    = st ?? "";
        ViewData["HideBotPlayers"] = _config.GetValue<bool>("HLStatsX:HideBotPlayers", true);
        ViewData["Games"] = await _search.GetVisibleGamesAsync(ct);

        if (string.IsNullOrWhiteSpace(q))
            return View("Index", (object?)null);

        var results = await _search.SearchAsync(q, string.IsNullOrEmpty(game) ? null : game, st, page, 50, ct);
        return View(results);
    }
}
