using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class PlayersController : Controller
{
    private readonly IPlayerService _players;
    private readonly IAwardService _awards;
    private readonly IConfiguration _config;

    public PlayersController(IPlayerService players, IAwardService awards, IConfiguration config)
    {
        _players = players;
        _awards = awards;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "skill", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var result = await _players.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, ct);
        return View(new PlayerLeaderboardViewModel(result, game, sortBy, desc));
    }

    public async Task<IActionResult> Profile(int id, CancellationToken ct)
    {
        var player = await _players.GetPlayerAsync(id, ct);
        if (player is null) return NotFound();

        var game = player.Game;
        var rankTask = _players.GetPlayerRankAsync(id, game, ct);
        var rankEntityTask = _awards.GetRankForPlayerAsync(id, game, ct);
        var aliasesTask = _players.GetPlayerAliasesAsync(id, ct);
        var awardsTask = _players.GetPlayerAwardsAsync(id, ct);
        var ribbonsTask = _players.GetPlayerRibbonsAsync(id, ct);

        await Task.WhenAll(rankTask, rankEntityTask, aliasesTask, awardsTask, ribbonsTask);

        return View(new PlayerProfileViewModel(
            player,
            await rankTask,
            await rankEntityTask,
            await aliasesTask,
            await awardsTask,
            await ribbonsTask));
    }

    public async Task<IActionResult> History(int id, int days = 30, CancellationToken ct = default)
    {
        var player = await _players.GetPlayerAsync(id, ct);
        if (player is null) return NotFound();

        var history = await _players.GetPlayerHistoryAsync(id, days, ct);
        return View(new PlayerHistoryViewModel(player, history, days));
    }

    public async Task<IActionResult> Bans(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var banned = await _players.GetBannedPlayersAsync(game, ct);
        return View(new BanListViewModel(banned, game));
    }
}
