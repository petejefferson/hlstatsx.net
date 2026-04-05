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

    public async Task<IActionResult> Index(string? q, string? game, int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View("Index", null);

        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var results = await _search.SearchAsync(q, game, page, 20, ct);
        return View(results);
    }
}
