using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class ClansController : Controller
{
    private readonly IClanService _clans;
    private readonly IConfiguration _config;

    public ClansController(IClanService clans, IConfiguration config)
    {
        _clans = clans;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "skill", bool desc = true, int minMembers = 1, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var result = await _clans.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, minMembers, ct);
        return View(new ClanLeaderboardViewModel(result, game, sortBy, desc, minMembers));
    }

    public async Task<IActionResult> Profile(int id, CancellationToken ct)
    {
        var clan = await _clans.GetClanAsync(id, ct);
        if (clan is null) return NotFound();

        var members = await _clans.GetMembersAsync(id, ct);
        return View(new ClanProfileViewModel(clan, members, clan.Game));
    }
}
