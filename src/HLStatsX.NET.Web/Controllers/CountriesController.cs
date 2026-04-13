using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class CountriesController : Controller
{
    private readonly ICountryService _countries;
    private readonly IConfiguration _config;

    public CountriesController(ICountryService countries, IConfiguration config)
    {
        _countries = countries;
        _config = config;
    }

    public async Task<IActionResult> Index(
        string? game, int page = 1,
        string sortBy = "skill", bool desc = true,
        int minMembers = 3,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var resultTask = _countries.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, minMembers, ct);
        var totalTask  = _countries.GetTotalCountAsync(game, ct);
        await Task.WhenAll(resultTask, totalTask);

        return View(new CountryLeaderboardViewModel(resultTask.Result, game, sortBy, desc, minMembers, totalTask.Result));
    }

    public async Task<IActionResult> Profile(
        string flag, string? game,
        int page = 1, string sortBy = "skill", bool desc = true,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var profile = await _countries.GetProfileAsync(flag, game, ct);
        if (profile is null) return NotFound();

        var members = await _countries.GetMembersAsync(flag, game, page, pageSize, sortBy, desc, ct);
        return View(new CountryProfileViewModel(profile, members, game, sortBy, desc));
    }
}
