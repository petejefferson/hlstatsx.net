using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
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

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "skill", bool desc = true, string rankType = "total", int minKills = 1, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        if (minKills < 1) minKills = 1;

        var ranks          = await _awards.GetRanksAsync(game, ct);
        var availableDates = await _players.GetHistoryDatesAsync(game, ct);

        PagedResult<PlayerLeaderboardRow> result;
        if (rankType == "total")
        {
            var players = await _players.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, minKills, ct);
            result = MapToRows(players);
        }
        else
        {
            var (from, to) = ParseRankType(rankType);
            result = await _players.GetPeriodLeaderboardAsync(game, from, to, page, pageSize, sortBy, desc, minKills, ct);
        }

        return View(new PlayerLeaderboardViewModel(result, game, sortBy, desc, ranks, rankType, availableDates, minKills));
    }

    private static PagedResult<PlayerLeaderboardRow> MapToRows(PagedResult<Player> players)
    {
        var rows = players.Items.Select(p => new PlayerLeaderboardRow
        {
            PlayerId       = p.PlayerId,
            LastName       = p.LastName,
            Flag           = p.Flag,
            Country        = p.Country,
            Clan           = p.Clan,
            ActivityScore  = p.ActivityScore,
            AllTimeKills   = p.Kills,
            Points         = p.Skill,
            Kills          = p.Kills,
            Deaths         = p.Deaths,
            Headshots      = p.Headshots,
            ConnectionTime = p.ConnectionTime,
            Shots          = p.Shots,
            Hits           = p.Hits
        }).ToList();
        return PagedResult<PlayerLeaderboardRow>.Create(rows, players.TotalCount, players.Page, players.PageSize);
    }

    private static (DateTime from, DateTime to) ParseRankType(string rankType)
    {
        var today = DateTime.UtcNow.Date;
        if (rankType == "week")  return (today.AddDays(-7),  today.AddDays(1));
        if (rankType == "month") return (today.AddDays(-30), today.AddDays(1));
        if (DateTime.TryParse(rankType, out var date))
            return (date.Date, date.Date.AddDays(1));
        return (DateTime.MinValue, DateTime.MaxValue);
    }

    public async Task<IActionResult> Profile(int id, CancellationToken ct)
    {
        var player = await _players.GetPlayerAsync(id, ct);
        if (player is null) return NotFound();

        var game = player.Game;

        // Fire all independent profile queries concurrently. Each service/repository
        // method creates its own short-lived DbContext via IDbContextFactory, so
        // concurrent access is safe.
        var rankTask          = _players.GetPlayerRankAsync(id, game, ct);
        // Pass kills directly — avoids a redundant player load inside the service
        var rankEntityTask    = _awards.GetRankForPlayerAsync(id, game, player.Kills, ct);
        var nextRankTask      = _players.GetNextRankAsync(game, player.Kills, ct);
        var allRanksTask      = _awards.GetRanksAsync(game, ct);
        var aliasesTask       = _players.GetPlayerAliasesAsync(id, ct);
        var awardsTask        = _players.GetPlayerAwardsAsync(id, ct);
        var allRibbonsTask    = _players.GetRibbonsWithStatusAsync(id, game, ct);
        var realStatsTask     = _players.GetRealStatsAsync(id, ct);
        var pingTask          = _players.GetAveragePingAsync(id, ct);
        var lastConnectTask   = _players.GetLastConnectAsync(id, ct);
        var favServerTask     = _players.GetFavoriteServerAsync(id, ct);
        var favMapTask        = _players.GetFavoriteMapAsync(id, ct);
        var favWeaponTask     = _players.GetFavoriteWeaponAsync(id, ct);
        var killStatsTask     = _players.GetKillStatsAsync(id, ct);
        var mapPerfTask       = _players.GetMapPerformanceAsync(id, ct);
        var serverPerfTask    = _players.GetServerPerformanceAsync(id, ct);
        var weaponStatsTask   = _players.GetWeaponStatsAsync(id, game, ct);
        var teamSelTask       = _players.GetTeamSelectionAsync(id, game, ct);
        var roleSelTask       = _players.GetRoleSelectionAsync(id, game, ct);
        var playerActionsTask = _players.GetPlayerActionsAsync(id, ct);
        var actionVictimsTask = _players.GetPlayerActionVictimsAsync(id, ct);

        await Task.WhenAll(
            rankTask, rankEntityTask, nextRankTask, allRanksTask,
            aliasesTask, awardsTask, allRibbonsTask, realStatsTask,
            pingTask, lastConnectTask, favServerTask, favMapTask,
            favWeaponTask, killStatsTask, mapPerfTask, serverPerfTask,
            weaponStatsTask, teamSelTask, roleSelTask,
            playerActionsTask, actionVictimsTask);

        var rankEntity = rankEntityTask.Result;
        var allRanks   = allRanksTask.Result;
        var pastRanks  = allRanks
            .Where(r => r.MinKills < (rankEntity?.MinKills ?? 0))
            .OrderBy(r => r.MinKills)
            .ToList();

        return View(new PlayerProfileViewModel
        {
            Player              = player,
            Rank                = rankTask.Result,
            CurrentRank         = rankEntity,
            NextRank            = nextRankTask.Result,
            PastRanks           = pastRanks,
            Aliases             = aliasesTask.Result,
            Awards              = awardsTask.Result,
            AllRibbons          = allRibbonsTask.Result,
            RealStats           = realStatsTask.Result,
            Ping                = pingTask.Result,
            LastConnect         = lastConnectTask.Result,
            FavoriteServer      = favServerTask.Result,
            FavoriteMap         = favMapTask.Result,
            FavoriteWeapon      = favWeaponTask.Result,
            KillStats           = killStatsTask.Result,
            MapPerformance      = mapPerfTask.Result,
            ServerPerformance   = serverPerfTask.Result,
            WeaponStats         = weaponStatsTask.Result,
            TeamSelection       = teamSelTask.Result,
            RoleSelection       = roleSelTask.Result,
            PlayerActions       = playerActionsTask.Result,
            PlayerActionVictims = actionVictimsTask.Result
        });
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
