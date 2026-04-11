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

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "skill", bool desc = true, string rankType = "total", CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var ranks          = await _awards.GetRanksAsync(game, ct);
        var availableDates = await _players.GetHistoryDatesAsync(game, ct);

        PagedResult<PlayerLeaderboardRow> result;
        if (rankType == "total")
        {
            var players = await _players.GetLeaderboardAsync(game, page, pageSize, sortBy, desc, ct);
            result = MapToRows(players);
        }
        else
        {
            var (from, to) = ParseRankType(rankType);
            result = await _players.GetPeriodLeaderboardAsync(game, from, to, page, pageSize, sortBy, desc, ct);
        }

        return View(new PlayerLeaderboardViewModel(result, game, sortBy, desc, ranks, rankType, availableDates));
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
        var rank = await _players.GetPlayerRankAsync(id, game, ct);
        var rankEntity = await _awards.GetRankForPlayerAsync(id, game, ct);
        var nextRank = await _players.GetNextRankAsync(game, player.Kills, ct);
        var allRanks = await _awards.GetRanksAsync(game, ct);
        var pastRanks = allRanks
            .Where(r => r.MinKills < (rankEntity?.MinKills ?? 0))
            .OrderBy(r => r.MinKills)
            .ToList();
        var aliases = await _players.GetPlayerAliasesAsync(id, ct);
        var awards = await _players.GetPlayerAwardsAsync(id, ct);
        var allRibbons = await _players.GetRibbonsWithStatusAsync(id, game, ct);
        var realStats = await _players.GetRealStatsAsync(id, ct);
        var ping = await _players.GetAveragePingAsync(id, ct);
        var lastConnect = await _players.GetLastConnectAsync(id, ct);
        var favServer = await _players.GetFavoriteServerAsync(id, ct);
        var favMap = await _players.GetFavoriteMapAsync(id, ct);
        var favWeapon = await _players.GetFavoriteWeaponAsync(id, ct);
        var killStats = await _players.GetKillStatsAsync(id, ct);
        var mapPerf = await _players.GetMapPerformanceAsync(id, ct);
        var serverPerf = await _players.GetServerPerformanceAsync(id, ct);
        var weaponStats = await _players.GetWeaponStatsAsync(id, game, ct);
        var teamSel = await _players.GetTeamSelectionAsync(id, game, ct);
        var roleSel = await _players.GetRoleSelectionAsync(id, game, ct);
        var playerActions = await _players.GetPlayerActionsAsync(id, ct);
        var playerActionVictims = await _players.GetPlayerActionVictimsAsync(id, ct);

        return View(new PlayerProfileViewModel(
            player, rank, rankEntity, nextRank, pastRanks, aliases, awards, allRibbons,
            realStats, ping, lastConnect, favServer, favMap, favWeapon,
            killStats, mapPerf, serverPerf, weaponStats, teamSel, roleSel,
            playerActions, playerActionVictims));
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
