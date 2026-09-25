using System.Text.RegularExpressions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

/// <summary>
/// Lightweight no-chrome pages served into the Half-Life in-game browser (MOTD window).
/// All actions use _InGameLayout — no navigation bar, no game dropdown.
/// </summary>
[Route("ingame")]
public class InGameController : Controller
{
    private readonly IPlayerService _players;
    private readonly IClanService _clans;
    private readonly IServerService _servers;
    private readonly IActionRepository _actions;
    private readonly IWeaponRepository _weapons;
    private readonly IMapRepository _maps;
    private readonly IConfiguration _config;

    public InGameController(
        IPlayerService players, IClanService clans, IServerService servers,
        IActionRepository actions, IWeaponRepository weapons, IMapRepository maps,
        IConfiguration config)
    {
        _players = players;
        _clans   = clans;
        _servers = servers;
        _actions = actions;
        _weapons = weapons;
        _maps    = maps;
        _config  = config;
    }

    private string DefaultGame => _config["HLStatsX:DefaultGame"] ?? "cstrike";

    private async Task<int?> ResolvePlayerIdAsync(int? player, string? uniqueid, string game, CancellationToken ct)
    {
        if (player.HasValue && player.Value > 0) return player.Value;
        if (!string.IsNullOrEmpty(uniqueid))
        {
            // Normalize STEAM_X:Y:Z → try as-is first, then strip STEAM_X: prefix
            var id = await _players.GetPlayerIdByUniqueIdAsync(uniqueid, game, ct);
            if (id == null)
            {
                var stripped = Regex.Replace(uniqueid, @"^STEAM_\d+:", "", RegexOptions.IgnoreCase);
                id = await _players.GetPlayerIdByUniqueIdAsync(stripped, game, ct);
            }
            return id;
        }
        return null;
    }

    [HttpGet("motd")]
    public async Task<IActionResult> Motd(
        string? game, int players = 10, int clans = 3, int servers = 9001,
        CancellationToken ct = default)
    {
        game    = (game ?? DefaultGame).Trim();
        players = Math.Max(0, Math.Min(players, 100));
        clans   = Math.Max(0, Math.Min(clans, 50));
        servers = Math.Max(0, Math.Min(servers, 9001));

        var serversTask = _servers.GetServersAsync(game, ct);

        var topPlayersTask = players > 0
            ? _players.GetLeaderboardAsync(game, 1, players, "skill", true, 1, ct)
            : Task.FromResult(PagedResult<Player>.Create([], 0, 1, 1));

        var topClansTask = clans > 0
            ? _clans.GetLeaderboardAsync(game, 1, clans, "skill", true, 1, ct)
            : Task.FromResult(PagedResult<ClanLeaderboardRow>.Create([], 0, 1, 1));

        await Task.WhenAll(serversTask, topPlayersTask, topClansTask);

        var serverList = servers > 0
            ? serversTask.Result.Take(servers).ToList()
            : [];

        return View(new InGameMotdViewModel(
            game,
            topPlayersTask.Result.Items,
            topClansTask.Result.Items,
            serverList));
    }

    [HttpGet("players")]
    public async Task<IActionResult> Players(
        string? game, string sortBy = "skill", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var result = await _players.GetLeaderboardAsync(game, page, 25, sortBy, desc, 1, ct);
        return View(new InGamePlayerListViewModel(game, result, sortBy, desc));
    }

    [HttpGet("clans")]
    public async Task<IActionResult> Clans(
        string? game, string sortBy = "skill", bool desc = true,
        int page = 1, int minMembers = 3,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var result = await _clans.GetLeaderboardAsync(game, page, 25, sortBy, desc, minMembers, ct);
        return View(new InGameClanListViewModel(game, result, sortBy, desc, minMembers));
    }

    [HttpGet("claninfo")]
    public async Task<IActionResult> ClanInfo(
        string? game, int clan, string memberSortBy = "skill", bool memberDesc = true,
        int page = 1, CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();

        var clanEntity = await _clans.GetClanAsync(clan, ct);
        if (clanEntity is null) return NotFound();

        var summaryTask = _clans.GetSummaryAsync(clan, ct);
        await summaryTask;
        var summary = summaryTask.Result;
        if (summary is null) return NotFound();

        var members = await _clans.GetMembersPagedAsync(
            clan, game, page, 20, memberSortBy, memberDesc, summary.TotalKills, ct);

        return View(new InGameClanInfoViewModel(game, clanEntity, summary, members, memberSortBy, memberDesc));
    }

    [HttpGet("statsme")]
    public async Task<IActionResult> PlayerStats(
        string? game, int? player, string? uniqueid,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var rankTask       = _players.GetPlayerRankAsync(playerId.Value, playerEntity.Game, ct);
        var realStatsTask  = _players.GetRealStatsAsync(playerId.Value, ct);
        var statsmeTask    = _players.GetWeaponStatsmeAsync(playerId.Value, playerEntity.Game, ct);
        await Task.WhenAll(rankTask, realStatsTask, statsmeTask);

        var statsme  = statsmeTask.Result;
        long smShots = statsme.Sum(w => w.Shots);
        long smHits  = statsme.Sum(w => w.Hits);
        double smAcc = smShots > 0 ? Math.Round((double)smHits / smShots * 100, 1) : 0;

        return View(new InGamePlayerStatsViewModel(
            game, playerEntity, rankTask.Result, realStatsTask.Result, smAcc, smShots, smHits));
    }

    [HttpGet("kills")]
    public async Task<IActionResult> Kills(
        string? game, int? player, string? uniqueid, int killLimit = 5,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var killStatsTask  = _players.GetKillStatsAsync(playerId.Value, killLimit, ct);
        var realStatsTask  = _players.GetRealStatsAsync(playerId.Value, ct);
        await Task.WhenAll(killStatsTask, realStatsTask);

        return View(new InGameKillsViewModel(
            game, playerEntity, killStatsTask.Result,
            realStatsTask.Result.RealHeadshots, killLimit));
    }

    [HttpGet("weapons")]
    public async Task<IActionResult> Weapons(
        string? game, int? player, string? uniqueid,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var weaponsTask   = _players.GetWeaponStatsAsync(playerId.Value, playerEntity.Game, ct);
        var realStatsTask = _players.GetRealStatsAsync(playerId.Value, ct);
        await Task.WhenAll(weaponsTask, realStatsTask);

        var realStats = realStatsTask.Result;
        return View(new InGameWeaponsViewModel(
            game, playerEntity, weaponsTask.Result,
            realStats.RealKills, realStats.RealHeadshots));
    }

    [HttpGet("accuracy")]
    public async Task<IActionResult> Accuracy(
        string? game, int? player, string? uniqueid,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var weapons = await _players.GetWeaponStatsmeAsync(playerId.Value, playerEntity.Game, ct);
        return View(new InGameAccuracyViewModel(game, playerEntity, weapons));
    }

    [HttpGet("targets")]
    public async Task<IActionResult> Targets(
        string? game, int? player, string? uniqueid,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var targets = await _players.GetWeaponTargetsAsync(playerId.Value, playerEntity.Game, ct);
        return View(new InGameTargetsViewModel(game, playerEntity, targets));
    }

    [HttpGet("maps")]
    public async Task<IActionResult> Maps(
        string? game, int? player, string? uniqueid,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var playerId = await ResolvePlayerIdAsync(player, uniqueid, game, ct);
        if (playerId is null) return BadRequest("No player ID specified.");

        var playerEntity = await _players.GetPlayerAsync(playerId.Value, ct);
        if (playerEntity is null) return NotFound();

        var mapsTask      = _players.GetMapPerformanceAsync(playerId.Value, ct);
        var realStatsTask = _players.GetRealStatsAsync(playerId.Value, ct);
        await Task.WhenAll(mapsTask, realStatsTask);

        var realStats = realStatsTask.Result;
        return View(new InGameMapsViewModel(
            game, playerEntity, mapsTask.Result,
            realStats.RealKills, realStats.RealHeadshots));
    }

    [HttpGet("servers")]
    public async Task<IActionResult> Servers(string? game, CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var serverList = await _servers.GetServersAsync(game, ct);
        return View(new InGameServersViewModel(game, serverList));
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(
        string? game, int? server_id,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();

        Server? server = null;
        if (server_id.HasValue)
        {
            server = await _servers.GetServerAsync(server_id.Value, ct);
        }
        else
        {
            var all = await _servers.GetServersAsync(game, ct);
            server = all.FirstOrDefault();
        }
        if (server is null) return NotFound();

        var liveTask      = _servers.GetLivestatsAsync(server.ServerId, ct);
        var gameStatsTask = _servers.GetGameStatsAsync(game, ct);
        var countTask     = _players.GetTotalCountAsync(game, ct);
        await Task.WhenAll(liveTask, gameStatsTask, countTask);

        var gs = gameStatsTask.Result;
        return View(new InGameStatusViewModel(
            game, server, liveTask.Result,
            countTask.Result, gs.TotalKills, gs.TotalHeadshots, gs.TotalServers));
    }

    [HttpGet("load")]
    public async Task<IActionResult> Load(
        string? game, int? server_id,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var gsTask    = _servers.GetGameStatsAsync(game, ct);
        var countTask = _players.GetTotalCountAsync(game, ct);
        await Task.WhenAll(gsTask, countTask);

        var gs = gsTask.Result;
        return View(new InGameLoadViewModel(
            game, countTask.Result, gs.TotalKills, gs.TotalHeadshots,
            gs.TotalServers, server_id ?? 0));
    }

    [HttpGet("bans")]
    public async Task<IActionResult> Bans(
        string? game, string sortBy = "last_event", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var result = await _players.GetBannedPlayersAsync(game, page, 25, sortBy, desc, 0, ct);
        return View(new InGameBansViewModel(game, result, sortBy, desc));
    }

    [HttpGet("help")]
    public async Task<IActionResult> Help(
        string? game, int? server_id,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var serverList = await _servers.GetServersAsync(game, ct);
        return View(new InGameHelpViewModel(game, serverList));
    }

    [HttpGet("weaponinfo")]
    public async Task<IActionResult> WeaponInfo(
        string? game, string? weapon,
        string sortBy = "frags", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        if (string.IsNullOrEmpty(weapon)) return BadRequest("No weapon specified.");

        var wep = await _weapons.GetByCodeAsync(weapon, game, ct);
        var weaponName = wep?.Name ?? char.ToUpperInvariant(weapon[0]) + weapon[1..];

        var killers = await _weapons.GetWeaponKillersAsync(weapon, game, page, 25, sortBy, desc, ct);
        return View(new InGameWeaponInfoViewModel(game, weapon, weaponName, killers, sortBy, desc));
    }

    [HttpGet("mapinfo")]
    public async Task<IActionResult> MapInfo(
        string? game, string? map,
        string sortBy = "kills", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        if (string.IsNullOrEmpty(map)) return BadRequest("No map specified.");

        var players = await _maps.GetPlayerLeaderboardAsync(map, game, page, 50, sortBy, desc, ct);
        return View(new InGameMapInfoViewModel(game, map, players, sortBy, desc));
    }

    [HttpGet("actions")]
    public async Task<IActionResult> Actions(
        string? game, string sortBy = "count", bool desc = true,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        var list = await _actions.GetListAsync(game, sortBy, desc, ct);
        return View(new InGameActionsViewModel(game, list, sortBy, desc));
    }

    [HttpGet("actioninfo")]
    public async Task<IActionResult> ActionInfo(
        string? game, string? action,
        string sortBy = "count", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game = (game ?? DefaultGame).Trim();
        if (string.IsNullOrEmpty(action)) return BadRequest("No action specified.");

        var actionEntity = await _actions.GetByCodeAsync(action, game, ct);
        var description = actionEntity?.Description ?? char.ToUpperInvariant(action[0]) + action[1..];

        bool usePlayerPlayer = actionEntity?.ForPlayerPlayerActions ?? false;
        var achievers = await _actions.GetAchieversAsync(action, game, usePlayerPlayer, page, 50, sortBy, desc, ct);

        return View(new InGameActionInfoViewModel(game, action, description, achievers, sortBy, desc, page));
    }
}
