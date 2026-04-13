using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Models.ViewModels;
using HLStatsX.NET.Web.Services;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace HLStatsX.NET.Web.Controllers;

public class PlayersController : Controller
{
    private readonly IPlayerService _players;
    private readonly IAwardService _awards;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ISteamService _steam;

    public PlayersController(IPlayerService players, IAwardService awards, IConfiguration config, IWebHostEnvironment env, ISteamService steam)
    {
        _players = players;
        _awards = awards;
        _config = config;
        _env = env;
        _steam = steam;
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

        return View(new PlayerLeaderboardViewModel(result, game, sortBy, desc, ranks, rankType, availableDates, minKills,
            _config.GetValue<bool>("HLStatsX:HideBotPlayers", true)));
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
            Hits           = p.Hits,
            IsBot          = p.IsBot
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

        if (_config.GetValue<bool>("HLStatsX:HideBotPlayers", true) && player.IsBot)
            return NotFound();

        var game = player.Game;

        var steamUniqueId = player.UniqueIds.FirstOrDefault()?.UniqueId;
        var steam64Id     = ISteamService.ToSteam64(steamUniqueId);

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
        var trendTask         = _players.GetTrendDataAsync(id, 30, ct);
        var globalAwardsTask  = _players.GetGlobalAwardsAsync(id, game, ct);

        await Task.WhenAll(
            rankTask, rankEntityTask, nextRankTask, allRanksTask,
            aliasesTask, awardsTask, allRibbonsTask, realStatsTask,
            pingTask, lastConnectTask, favServerTask, favMapTask,
            favWeaponTask, killStatsTask, mapPerfTask, serverPerfTask,
            weaponStatsTask, teamSelTask, roleSelTask,
            playerActionsTask, actionVictimsTask, trendTask, globalAwardsTask);

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
            PlayerActionVictims = actionVictimsTask.Result,
            TrendData           = trendTask.Result,
            GlobalAwards        = globalAwardsTask.Result,
            Steam64Id           = steam64Id,
            HideBotPlayers      = _config.GetValue<bool>("HLStatsX:HideBotPlayers", true)
        });
    }

    public async Task<IActionResult> Avatar(long steam64, CancellationToken ct)
    {
        var avatarUrl = await _steam.GetAvatarUrlAsync(steam64, ct);
        if (string.IsNullOrEmpty(avatarUrl))
            return NotFound();
        return Redirect(avatarUrl);
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

    /// <summary>
    /// Generates a 400x75 forum signature PNG image for the given player.
    /// URL: /Players/Sig/{id}
    /// </summary>
    public async Task<IActionResult> Sig(int id, CancellationToken ct)
    {
        var player = await _players.GetPlayerAsync(id, ct);
        if (player is null) return NotFound();

        var rankTask  = _players.GetPlayerRankAsync(id, player.Game, ct);
        var countTask = _players.GetTotalCountAsync(player.Game, ct);
        await Task.WhenAll(rankTask, countTask);

        int rank  = rankTask.Result;
        int total = countTask.Result;

        var bytes = GenerateSigPng(player, rank, total);
        return File(bytes, "image/png");
    }

    private byte[] GenerateSigPng(Player player, int rank, int totalPlayers)
    {
        const int Width  = 400;
        const int Height = 75;

        // Pick a deterministic background (1-11) based on player id
        int bgNumber = (player.PlayerId % 11) + 1;

        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;

        // Draw background image if available
        var bgPath = Path.Combine(_env.WebRootPath, "hlstatsimg", "games", player.Game,
                                   "sig", $"{bgNumber}.png");
        if (!System.IO.File.Exists(bgPath))
            bgPath = Path.Combine(_env.WebRootPath, "hlstatsimg", "sig", $"{bgNumber}.png");

        if (System.IO.File.Exists(bgPath))
        {
            using var bgBitmap = SKBitmap.Decode(bgPath);
            if (bgBitmap is not null)
                canvas.DrawBitmap(bgBitmap, new SKRect(0, 0, Width, Height));
        }
        else
        {
            canvas.Clear(new SKColor(30, 30, 30));
        }

        // Load font
        var fontPath = Path.Combine(_env.WebRootPath, "hlstatsimg", "sig", "font", "DejaVuSans.ttf");
        using var typeface = System.IO.File.Exists(fontPath)
            ? SKTypeface.FromFile(fontPath)
            : SKTypeface.Default;

        // Determine text colours based on background (mirrors PHP switch)
        var (captionRgb, textRgb) = bgNumber switch
        {
            1  => ((0,   0,   255), (0,   0,   0)),
            2  => ((147, 23,  18),  (255, 255, 255)),
            3  => ((150, 180, 99),  (255, 255, 255)),
            4  => ((255, 203, 4),   (255, 255, 255)),
            5  => ((255, 255, 255), (255, 255, 255)),
            6  => ((0,   0,   0),   (255, 255, 255)),
            7  => ((255, 255, 255), (0,   0,   0)),
            8  => ((255, 255, 255), (255, 255, 255)),
            9  => ((255, 255, 255), (0,   0,   0)),
            10 => ((255, 255, 255), (255, 255, 255)),
            11 => ((150, 180, 99),  (255, 255, 255)),
            _  => ((0,   0,   255), (0,   0,   0))
        };

        using var captionFont = new SKFont(typeface, 12) { Edging = SKFontEdging.Antialias };
        using var textFont    = new SKFont(typeface, 10) { Edging = SKFontEdging.Antialias };
        using var captionPaint = new SKPaint
        {
            Color       = new SKColor((byte)captionRgb.Item1, (byte)captionRgb.Item2, (byte)captionRgb.Item3),
            IsAntialias = true
        };
        using var textPaint = new SKPaint
        {
            Color       = new SKColor((byte)textRgb.Item1, (byte)textRgb.Item2, (byte)textRgb.Item3),
            IsAntialias = true
        };

        // Draw player flag
        int nameX = 9;
        if (!string.IsNullOrEmpty(player.Flag))
        {
            var flagPath = Path.Combine(_env.WebRootPath, "hlstatsimg", "flags",
                                         $"{player.Flag.ToLower()}.gif");
            if (System.IO.File.Exists(flagPath))
            {
                using var flagBmp = SKBitmap.Decode(flagPath);
                if (flagBmp is not null)
                {
                    canvas.DrawBitmap(flagBmp, new SKRect(8, 4, 26, 16));
                    nameX += 20;
                }
            }
        }

        // Player name
        var displayName = player.LastName.Length > 30
            ? player.LastName[..27] + "..."
            : player.LastName;
        canvas.DrawText(displayName, nameX, 15, SKTextAlign.Left, captionFont, captionPaint);

        // Rank line
        var rankText = $"Rank: {rank:N0} of {totalPlayers:N0} with {player.Skill:N0} pts";
        canvas.DrawText(rankText, 15, 30, SKTextAlign.Left, textFont, textPaint);

        // Frags line
        double kpd = player.Deaths == 0 ? player.Kills : (double)player.Kills / player.Deaths;
        int hpk    = player.Kills == 0 ? 0 : (int)Math.Round((double)player.Headshots / player.Kills * 100);
        canvas.DrawText(
            $"Frags: {player.Kills:N0} kills / {player.Deaths:N0} deaths ({kpd:F2} K:D), {player.Headshots:N0} headshots ({hpk}%)",
            15, 43, SKTextAlign.Left, textFont, textPaint);

        // Activity line
        var ts   = TimeSpan.FromSeconds(player.ConnectionTime);
        var time = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        canvas.DrawText($"Activity: {player.ActivityScore}%   Time: {time} hours", 15, 56, SKTextAlign.Left, textFont, textPaint);

        // Site name
        var siteName = _config["HLStatsX:SiteName"] ?? "HLStatsX.NET";
        canvas.DrawText($"Stats: {siteName}", 15, 68, SKTextAlign.Left, textFont, textPaint);

        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
