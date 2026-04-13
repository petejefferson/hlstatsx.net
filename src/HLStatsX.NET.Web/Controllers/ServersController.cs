using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class ServersController : Controller
{
    private readonly IServerService _servers;
    private readonly IPlayerService _players;
    private readonly IConfiguration _config;

    public ServersController(IServerService servers, IPlayerService players, IConfiguration config)
    {
        _servers = servers;
        _players = players;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";

        var serversTask     = _servers.GetServersAsync(game, ct);
        var gameStatsTask   = _servers.GetGameStatsAsync(game, ct);
        var playerCountTask = _players.GetTotalCountAsync(game, ct);
        await Task.WhenAll(serversTask, gameStatsTask, playerCountTask);

        var gameStats    = gameStatsTask.Result;
        var playerCount  = playerCountTask.Result;
        var newPlayers24h = gameStats.Trend24hPlayers >= 0 ? playerCount - gameStats.Trend24hPlayers : -1;

        return View(new ServerListViewModel(serversTask.Result, game, playerCount, newPlayers24h, gameStats));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var server = await _servers.GetServerAsync(id, ct);
        if (server is null) return NotFound();

        var livestats = await _servers.GetLivestatsAsync(id, ct);
        return View(new ServerDetailViewModel(server, livestats));
    }
}
