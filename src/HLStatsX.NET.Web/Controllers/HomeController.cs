using System.Diagnostics;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPlayerService _playerService;
    private readonly IServerService _serverService;
    private readonly IAwardService _awardService;
    private readonly IGameService _gameService;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public HomeController(IPlayerService playerService, IServerService serverService,
        IAwardService awardService, IGameService gameService,
        IAdminService adminService, IConfiguration config)
    {
        _playerService = playerService;
        _serverService = serverService;
        _awardService = awardService;
        _gameService = gameService;
        _adminService = adminService;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(game))
        {
            var data = await _gameService.GetGamesListAsync(ct);
            if (data.Games.Count == 1)
                game = data.Games[0].Code;
            else
                return View("GamesList", data);
        }

        var opts = await _adminService.GetOptionsAsync(ct);
        bool showMap = opts.TryGetValue("show_google_map", out var mapOpt) && mapOpt == "1";
        DateTime? dailyAwardsDate = opts.TryGetValue("awards_d_date", out var dStr) &&
            DateTime.TryParse(dStr, out var d) ? d.Date : null;

        var servers        = await _serverService.GetServersAsync(game, ct);

        if (servers.Count == 1)
            return RedirectToAction("Detail", "Servers", new { id = servers[0].ServerId });

        var playerCountTask  = _playerService.GetTotalCountAsync(game, ct);
        var gameStatsTask    = _serverService.GetGameStatsAsync(game, ct);
        var livestatsTask    = _serverService.GetAllLivestatsAsync(game, ct);
        var dailyAwardsTask  = _awardService.GetDailyAwardsAsync(game, ct);
        var serverLoadTask   = _serverService.GetServerLoadAsync(game, 24, ct);
        var trendSeriesTask  = _serverService.GetTrendSeriesAsync(game, 24, ct);
        var teamsTask        = _serverService.GetTeamsAsync(game, ct);
        var voiceCommTask    = _serverService.GetVoiceCommServersAsync(ct);
        var locationsTask    = showMap
            ? _serverService.GetOnlinePlayerLocationsAsync(game, ct)
            : Task.FromResult<IReadOnlyList<Core.Models.OnlinePlayerLocation>>([]);

        await Task.WhenAll(playerCountTask, gameStatsTask, livestatsTask, dailyAwardsTask,
            serverLoadTask, trendSeriesTask, teamsTask, voiceCommTask, locationsTask);

        var playerCount  = await playerCountTask;
        var gameStats    = await gameStatsTask;
        var teams        = (await teamsTask).ToDictionary(t => t.Code, t => t);

        var newPlayers24h = gameStats.Trend24hPlayers >= 0
            ? playerCount - gameStats.Trend24hPlayers
            : -1;

        var model = new HomeViewModel(
            game,
            playerCount,
            newPlayers24h,
            gameStats,
            servers,
            await livestatsTask,
            await dailyAwardsTask,
            dailyAwardsDate,
            await serverLoadTask,
            teams,
            await trendSeriesTask,
            await voiceCommTask,
            await locationsTask,
            showMap);

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext?.TraceIdentifier));

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusPage(int id)
    {
        (string title, string description) = id switch
        {
            404 => ("Page Not Found", "The page you requested could not be found."),
            403 => ("Access Denied", "You do not have permission to view this page."),
            429 => ("Too Many Requests", "Too many requests have been received from your address. Please wait a moment and try again."),
            _   => ($"Error {id}", "An error occurred while processing your request.")
        };
        return View(new StatusPageViewModel(id, title, description));
    }
}
