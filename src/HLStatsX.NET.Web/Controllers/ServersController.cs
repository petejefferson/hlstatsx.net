using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HLStatsX.NET.Web.Controllers;

/// <summary>
/// Handles the server list and server detail pages, including live player counts,
/// server load charts, and daily awards.
/// </summary>
public class ServersController : Controller
{
    private readonly IServerService _servers;
    private readonly IPlayerService _players;
    private readonly IAwardService _awards;
    private readonly IAdminService _admin;
    private readonly IConfiguration _config;

    public ServersController(IServerService servers, IPlayerService players, IAwardService awards, IAdminService admin, IConfiguration config)
    {
        _servers = servers;
        _players = players;
        _awards  = awards;
        _admin   = admin;
        _config  = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";

        var serversTask     = _servers.GetServersAsync(game, ct);
        var gameStatsTask   = _servers.GetGameStatsAsync(game, ct);
        var playerCountTask = _players.GetTotalCountAsync(game, ct);
        var trendTask       = _servers.GetTrendSeriesAsync(game, 24, ct);
        await Task.WhenAll(serversTask, gameStatsTask, playerCountTask, trendTask);

        var gameStats    = gameStatsTask.Result;
        var playerCount  = playerCountTask.Result;
        var newPlayers24h = gameStats.Trend24hPlayers >= 0 ? playerCount - gameStats.Trend24hPlayers : -1;

        return View(new ServerListViewModel(serversTask.Result, game, playerCount, newPlayers24h, gameStats, trendTask.Result));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var server = await _servers.GetServerAsync(id, ct);
        if (server is null) return NotFound();

        var game = server.Game;
        ViewData["Game"] = game;

        var livestatsTask   = _servers.GetLivestatsAsync(id, ct);
        var serverLoadTask  = _servers.GetServerLoadByServerIdAsync(id, 24, ct);
        var gameStatsTask   = _servers.GetGameStatsAsync(game, ct);
        var playerCountTask = _players.GetTotalCountAsync(game, ct);
        var dailyAwardsTask = _awards.GetDailyAwardsAsync(game, ct);
        var trendTask       = _servers.GetTrendSeriesAsync(game, 24, ct);
        var teamsTask       = _servers.GetTeamsAsync(game, ct);
        var optsTask        = _admin.GetOptionsAsync(ct);
        await Task.WhenAll(livestatsTask, serverLoadTask, gameStatsTask, playerCountTask, dailyAwardsTask, trendTask, teamsTask, optsTask);

        var gameStats     = gameStatsTask.Result;
        var playerCount   = playerCountTask.Result;
        var newPlayers24h = gameStats.Trend24hPlayers >= 0 ? playerCount - gameStats.Trend24hPlayers : -1;
        var teams         = teamsTask.Result.ToDictionary(t => t.Code, t => t);
        var opts          = optsTask.Result;
        DateTime? dailyAwardsDate = opts.TryGetValue("awards_d_date", out var dStr) &&
            DateTime.TryParse(dStr, out var d) ? d.Date : null;

        return View(new ServerDetailViewModel(
            server, game, playerCount, newPlayers24h, gameStats,
            livestatsTask.Result, dailyAwardsTask.Result, dailyAwardsDate, serverLoadTask.Result,
            teams, trendTask.Result));
    }

    public async Task<IActionResult> LoadChart(int id, int range = 1, CancellationToken ct = default)
    {
        range = range is >= 1 and <= 4 ? range : 1;
        var rows = await _servers.GetServerLoadRangedAsync(id, range, ct);

        string fmt = range switch
        {
            2 => "ddd HH:mm",
            3 => "MM/dd",
            4 => "MMM dd",
            _ => "HH:mm"
        };

        var data = rows.Select(r => new
        {
            label      = r.DateTime.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture),
            actPlayers = r.ActPlayers,
            maxPlayers = r.MaxPlayers,
            map        = r.Map ?? ""
        });
        return Json(data);
    }

    public async Task<IActionResult> Livestats(int id, CancellationToken ct)
    {
        var server = await _servers.GetServerAsync(id, ct);
        if (server is null) return NotFound();

        var livestatsTask = _servers.GetLivestatsAsync(id, ct);
        var teamsTask     = _servers.GetTeamsAsync(server.Game, ct);
        await Task.WhenAll(livestatsTask, teamsTask);

        var teams = teamsTask.Result.ToDictionary(t => t.Code, t => t);
        return View(new ServerLivestatsViewModel(server, server.Game, livestatsTask.Result, teams));
    }
}
