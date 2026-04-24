using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
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
        string weaponsSortBy = "kills", bool weaponsDesc = true,
        string weaponStatsSortBy = "kills", bool weaponStatsDesc = true,
        string weaponTargetsSortBy = "hits", bool weaponTargetsDesc = true,
        string mapsSortBy = "kills", bool mapsDesc = true,
        string actionsSortBy = "count", bool actionsDesc = true,
        string victimsSortBy = "count", bool victimsDesc = true,
        string teamsSortBy = "joined", bool teamsDesc = true,
        string rolesSortBy = "joined", bool rolesDesc = true,
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
        var weaponStatsTask    = _clans.GetWeaponStatsAsync(id, clan.Game, ct);
        var weaponTargetsTask  = _clans.GetWeaponTargetsAsync(id, clan.Game, ct);
        var mapsTask           = _clans.GetMapPerformanceAsync(id, summary.TotalKills, summary.TotalHeadshots, ct);
        var actionsTask        = _clans.GetActionsAsync(id, ct);
        var actionVictimsTask  = _clans.GetActionVictimsAsync(id, ct);
        var teamsTask          = _clans.GetTeamSelectionAsync(id, clan.Game, ct);
        var rolesTask          = _clans.GetRoleSelectionAsync(id, clan.Game, ct);
        var locationsTask      = _clans.GetMemberLocationsAsync(id, ct);

        await Task.WhenAll(
            favServerTask, favMapTask, favWeaponTask, membersTask,
            weaponsTask, weaponStatsTask, weaponTargetsTask,
            mapsTask, actionsTask, actionVictimsTask, teamsTask, rolesTask, locationsTask);

        string? googleMapsApiKey = _config["HLStatsX:Maps:GoogleMapsApiKey"];
        if (string.IsNullOrWhiteSpace(googleMapsApiKey)) googleMapsApiKey = null;

        return View(new ClanProfileViewModel(
            clan, summary,
            favServerTask.Result, favMapTask.Result, favWeaponTask.Result,
            membersTask.Result, membersPage, membersSortBy, membersDesc,
            SortWeapons(weaponsTask.Result, weaponsSortBy, weaponsDesc), weaponsSortBy, weaponsDesc,
            SortWeaponStats(weaponStatsTask.Result, weaponStatsSortBy, weaponStatsDesc), weaponStatsSortBy, weaponStatsDesc,
            SortWeaponTargets(weaponTargetsTask.Result, weaponTargetsSortBy, weaponTargetsDesc), weaponTargetsSortBy, weaponTargetsDesc,
            SortMaps(mapsTask.Result, mapsSortBy, mapsDesc), mapsSortBy, mapsDesc,
            SortActions(actionsTask.Result, actionsSortBy, actionsDesc), actionsSortBy, actionsDesc,
            SortActions(actionVictimsTask.Result, victimsSortBy, victimsDesc), victimsSortBy, victimsDesc,
            SortTeams(teamsTask.Result, teamsSortBy, teamsDesc), teamsSortBy, teamsDesc,
            SortRoles(rolesTask.Result, rolesSortBy, rolesDesc), rolesSortBy, rolesDesc,
            locationsTask.Result, googleMapsApiKey));
    }

    private static IReadOnlyList<ClanWeaponRow> SortWeapons(IReadOnlyList<ClanWeaponRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanWeaponRow> q = sortBy switch
        {
            "name"      => desc ? rows.OrderByDescending(r => r.WeaponName)      : rows.OrderBy(r => r.WeaponName),
            "modifier"  => desc ? rows.OrderByDescending(r => r.Modifier)        : rows.OrderBy(r => r.Modifier),
            "killpct"   => desc ? rows.OrderByDescending(r => r.KillPercent)     : rows.OrderBy(r => r.KillPercent),
            "headshots" => desc ? rows.OrderByDescending(r => r.Headshots)      : rows.OrderBy(r => r.Headshots),
            "hspct"     => desc ? rows.OrderByDescending(r => r.HeadshotPercent) : rows.OrderBy(r => r.HeadshotPercent),
            "hsperkill" => desc ? rows.OrderByDescending(r => r.HeadshotsPerKill): rows.OrderBy(r => r.HeadshotsPerKill),
            _           => desc ? rows.OrderByDescending(r => r.Kills)           : rows.OrderBy(r => r.Kills),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanWeaponStatsRow> SortWeaponStats(IReadOnlyList<ClanWeaponStatsRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanWeaponStatsRow> q = sortBy switch
        {
            "name"      => desc ? rows.OrderByDescending(r => r.WeaponName)    : rows.OrderBy(r => r.WeaponName),
            "shots"     => desc ? rows.OrderByDescending(r => r.Shots)         : rows.OrderBy(r => r.Shots),
            "hits"      => desc ? rows.OrderByDescending(r => r.Hits)          : rows.OrderBy(r => r.Hits),
            "damage"    => desc ? rows.OrderByDescending(r => r.Damage)        : rows.OrderBy(r => r.Damage),
            "headshots" => desc ? rows.OrderByDescending(r => r.Headshots)     : rows.OrderBy(r => r.Headshots),
            "deaths"    => desc ? rows.OrderByDescending(r => r.Deaths)        : rows.OrderBy(r => r.Deaths),
            "kdr"       => desc ? rows.OrderByDescending(r => r.KillDeathRatio): rows.OrderBy(r => r.KillDeathRatio),
            "accuracy"  => desc ? rows.OrderByDescending(r => r.Accuracy)      : rows.OrderBy(r => r.Accuracy),
            "dph"       => desc ? rows.OrderByDescending(r => r.DamagePerHit)  : rows.OrderBy(r => r.DamagePerHit),
            "spk"       => desc ? rows.OrderByDescending(r => r.ShotsPerKill)  : rows.OrderBy(r => r.ShotsPerKill),
            _           => desc ? rows.OrderByDescending(r => r.Kills)         : rows.OrderBy(r => r.Kills),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanWeaponTargetRow> SortWeaponTargets(IReadOnlyList<ClanWeaponTargetRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanWeaponTargetRow> q = sortBy switch
        {
            "name"     => desc ? rows.OrderByDescending(r => r.WeaponName) : rows.OrderBy(r => r.WeaponName),
            "head"     => desc ? rows.OrderByDescending(r => r.Head)       : rows.OrderBy(r => r.Head),
            "chest"    => desc ? rows.OrderByDescending(r => r.Chest)      : rows.OrderBy(r => r.Chest),
            "stomach"  => desc ? rows.OrderByDescending(r => r.Stomach)    : rows.OrderBy(r => r.Stomach),
            "leftarm"  => desc ? rows.OrderByDescending(r => r.LeftArm)    : rows.OrderBy(r => r.LeftArm),
            "rightarm" => desc ? rows.OrderByDescending(r => r.RightArm)   : rows.OrderBy(r => r.RightArm),
            "leftleg"  => desc ? rows.OrderByDescending(r => r.LeftLeg)    : rows.OrderBy(r => r.LeftLeg),
            "rightleg" => desc ? rows.OrderByDescending(r => r.RightLeg)   : rows.OrderBy(r => r.RightLeg),
            "left"     => desc ? rows.OrderByDescending(r => r.LeftPct)    : rows.OrderBy(r => r.LeftPct),
            "middle"   => desc ? rows.OrderByDescending(r => r.MiddlePct)  : rows.OrderBy(r => r.MiddlePct),
            "right"    => desc ? rows.OrderByDescending(r => r.RightPct)   : rows.OrderBy(r => r.RightPct),
            _          => desc ? rows.OrderByDescending(r => r.Hits)       : rows.OrderBy(r => r.Hits),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanMapRow> SortMaps(IReadOnlyList<ClanMapRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanMapRow> q = sortBy switch
        {
            "map"       => desc ? rows.OrderByDescending(r => r.Map)             : rows.OrderBy(r => r.Map),
            "killpct"   => desc ? rows.OrderByDescending(r => r.KillPercent)     : rows.OrderBy(r => r.KillPercent),
            "deaths"    => desc ? rows.OrderByDescending(r => r.Deaths)          : rows.OrderBy(r => r.Deaths),
            "kd"        => desc ? rows.OrderByDescending(r => r.KillDeathRatio)  : rows.OrderBy(r => r.KillDeathRatio),
            "headshots" => desc ? rows.OrderByDescending(r => r.Headshots)      : rows.OrderBy(r => r.Headshots),
            "hspct"     => desc ? rows.OrderByDescending(r => r.HeadshotPercent) : rows.OrderBy(r => r.HeadshotPercent),
            "hsperkill" => desc ? rows.OrderByDescending(r => r.HeadshotsPerKill): rows.OrderBy(r => r.HeadshotsPerKill),
            _           => desc ? rows.OrderByDescending(r => r.Kills)           : rows.OrderBy(r => r.Kills),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanActionRow> SortActions(IReadOnlyList<ClanActionRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanActionRow> q = sortBy switch
        {
            "desc"  => desc ? rows.OrderByDescending(r => r.Description) : rows.OrderBy(r => r.Description),
            "bonus" => desc ? rows.OrderByDescending(r => r.TotalBonus)  : rows.OrderBy(r => r.TotalBonus),
            _       => desc ? rows.OrderByDescending(r => r.Count)       : rows.OrderBy(r => r.Count),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanTeamRow> SortTeams(IReadOnlyList<ClanTeamRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanTeamRow> q = sortBy switch
        {
            "team"    => desc ? rows.OrderByDescending(r => r.TeamName)  : rows.OrderBy(r => r.TeamName),
            "percent" => desc ? rows.OrderByDescending(r => r.Percent)   : rows.OrderBy(r => r.Percent),
            _         => desc ? rows.OrderByDescending(r => r.JoinCount) : rows.OrderBy(r => r.JoinCount),
        };
        return q.ToList();
    }

    private static IReadOnlyList<ClanRoleRow> SortRoles(IReadOnlyList<ClanRoleRow> rows, string sortBy, bool desc)
    {
        IOrderedEnumerable<ClanRoleRow> q = sortBy switch
        {
            "role"    => desc ? rows.OrderByDescending(r => r.RoleName)       : rows.OrderBy(r => r.RoleName),
            "percent" => desc ? rows.OrderByDescending(r => r.Percent)        : rows.OrderBy(r => r.Percent),
            "kills"   => desc ? rows.OrderByDescending(r => r.Kills)          : rows.OrderBy(r => r.Kills),
            "deaths"  => desc ? rows.OrderByDescending(r => r.Deaths)         : rows.OrderBy(r => r.Deaths),
            "kd"      => desc ? rows.OrderByDescending(r => r.KillDeathRatio) : rows.OrderBy(r => r.KillDeathRatio),
            _         => desc ? rows.OrderByDescending(r => r.JoinCount)      : rows.OrderBy(r => r.JoinCount),
        };
        return q.ToList();
    }
}
