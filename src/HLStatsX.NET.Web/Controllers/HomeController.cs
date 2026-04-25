using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPlayerService _playerService;
    private readonly IServerService _serverService;
    private readonly IAwardService _awardService;
    private readonly IConfiguration _config;

    public HomeController(IPlayerService playerService, IServerService serverService,
        IAwardService awardService, IConfiguration config)
    {
        _playerService = playerService;
        _serverService = serverService;
        _awardService = awardService;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";

        var servers      = await _serverService.GetServersAsync(game, ct);
        var playerCount  = await _playerService.GetTotalCountAsync(game, ct);
        var gameStats    = await _serverService.GetGameStatsAsync(game, ct);
        var livestats    = await _serverService.GetAllLivestatsAsync(game, ct);
        var dailyAwards  = await _awardService.GetDailyAwardsAsync(game, ct);
        var serverLoad   = await _serverService.GetServerLoadAsync(game, 100, ct);
        var trendSeries  = await _serverService.GetTrendSeriesAsync(game, 24, ct);
        var teamsList    = await _serverService.GetTeamsAsync(game, ct);
        var teams        = teamsList.ToDictionary(t => t.Code, t => t);

        // new players last 24h: current total minus the Trend snapshot 24h ago
        var newPlayers24h = gameStats.Trend24hPlayers >= 0
            ? playerCount - gameStats.Trend24hPlayers
            : -1;

        var model = new HomeViewModel(
            game,
            playerCount,
            newPlayers24h,
            gameStats,
            servers,
            livestats,
            dailyAwards,
            serverLoad,
            teams,
            trendSeries);

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
