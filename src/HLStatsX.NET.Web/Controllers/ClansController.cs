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
        var leaderboardTask = _clans.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, minMembers, ct);
        var totalTask = _clans.GetTotalCountAsync(game, ct);
        await Task.WhenAll(leaderboardTask, totalTask);
        return View(new ClanLeaderboardViewModel(leaderboardTask.Result, game, sortBy, desc, minMembers, totalTask.Result));
    }

    public async Task<IActionResult> Profile(
        int id,
        int membersPage = 1, string membersSortBy = "skill", bool membersDesc = true,
        CancellationToken ct = default)
    {
        var clan = await _clans.GetClanAsync(id, ct);
        if (clan is null) return NotFound();

        var summary = await _clans.GetSummaryAsync(id, ct);
        if (summary is null) return NotFound();

        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var favServerTask      = _clans.GetFavoriteServerAsync(id, ct);
        var favMapTask         = _clans.GetFavoriteMapAsync(id, ct);
        var favWeaponTask      = _clans.GetFavoriteWeaponAsync(id, clan.Game, ct);
        var membersTask        = _clans.GetMembersPagedAsync(id, clan.Game, membersPage, pageSize, membersSortBy, membersDesc, summary.TotalKills, ct);
        var weaponsTask        = _clans.GetWeaponUsageAsync(id, clan.Game, summary.TotalKills, summary.TotalHeadshots, ct);
        var mapsTask           = _clans.GetMapPerformanceAsync(id, summary.TotalKills, summary.TotalHeadshots, ct);
        var actionsTask        = _clans.GetActionsAsync(id, ct);
        var actionVictimsTask  = _clans.GetActionVictimsAsync(id, ct);
        var teamsTask          = _clans.GetTeamSelectionAsync(id, clan.Game, ct);
        var rolesTask          = _clans.GetRoleSelectionAsync(id, clan.Game, ct);

        await Task.WhenAll(
            favServerTask, favMapTask, favWeaponTask, membersTask,
            weaponsTask, mapsTask, actionsTask, actionVictimsTask, teamsTask, rolesTask);

        return View(new ClanProfileViewModel(
            clan, summary,
            favServerTask.Result, favMapTask.Result, favWeaponTask.Result,
            membersTask.Result, membersPage, membersSortBy, membersDesc,
            weaponsTask.Result, mapsTask.Result,
            actionsTask.Result, actionVictimsTask.Result,
            teamsTask.Result, rolesTask.Result));
    }
}
