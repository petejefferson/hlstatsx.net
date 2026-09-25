using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using HLStatsX.NET.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class PlayersControllerTests
{
    private readonly Mock<IPlayerService> _playerServiceMock;
    private readonly Mock<IAwardService> _awardServiceMock;
    private readonly IConfiguration _config;
    private readonly PlayersController _controller;

    public PlayersControllerTests()
    {
        _playerServiceMock = new Mock<IPlayerService>();
        _awardServiceMock = new Mock<IAwardService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["HLStatsX:DefaultGame"] = "cstrike",
            ["HLStatsX:DefaultPageSize"] = "50"
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(string.Empty);

        var steamMock = new Mock<ISteamService>();
        steamMock.Setup(s => s.GetAvatarUrlAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string?)null);

        _controller = new PlayersController(_playerServiceMock.Object, _awardServiceMock.Object, _config, envMock.Object, steamMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithLeaderboard()
    {
        var players = new List<Player>
        {
            new() { PlayerId = 1, LastName = "TopPlayer", Game = "cstrike", Skill = 2000, Kills = 500 }
        };
        var paged = PagedResult<Player>.Create(players, 1, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default))
            .ReturnsAsync(paged);
        _playerServiceMock
            .Setup(s => s.GetHistoryDatesAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<DateTime>());
        _awardServiceMock
            .Setup(s => s.GetRanksAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<Rank>());

        var result = await _controller.Index(null, 1, "skill", true);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PlayerLeaderboardViewModel>().Subject;
        model.Players.Items.Should().HaveCount(1);
        model.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playerServiceMock.Setup(s => s.GetPlayerAsync(999, default)).ReturnsAsync((Player?)null);

        var result = await _controller.Profile(999, ct: default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Profile_ReturnsView_WhenPlayerExists()
    {
        var player = new Player { PlayerId = 1, LastName = "FragMaster", Game = "cstrike", Kills = 500 };

        _playerServiceMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playerServiceMock.Setup(s => s.GetPlayerRankAsync(1, "cstrike", default)).ReturnsAsync(1);
        _playerServiceMock.Setup(s => s.GetNextRankAsync("cstrike", 500, default)).ReturnsAsync((Rank?)null);
        _playerServiceMock.Setup(s => s.GetPlayerAliasesAsync(1, default)).ReturnsAsync(Array.Empty<PlayerName>());
        _playerServiceMock.Setup(s => s.GetPlayerAwardsAsync(1, default)).ReturnsAsync(Array.Empty<PlayerAward>());
        _playerServiceMock.Setup(s => s.GetPlayerRibbonsAsync(1, default)).ReturnsAsync(Array.Empty<PlayerRibbon>());
        _playerServiceMock.Setup(s => s.GetRibbonsWithStatusAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<RibbonDisplay>());
        _playerServiceMock.Setup(s => s.GetRealStatsAsync(1, default)).ReturnsAsync(new RealStats(0, 0, 0, 0, 0, 0));
        _playerServiceMock.Setup(s => s.GetAveragePingAsync(1, default)).ReturnsAsync((PingStats?)null);
        _playerServiceMock.Setup(s => s.GetLastConnectAsync(1, default)).ReturnsAsync((DateTime?)null);
        _playerServiceMock.Setup(s => s.GetFavoriteServerAsync(1, default)).ReturnsAsync((FavoriteServer?)null);
        _playerServiceMock.Setup(s => s.GetFavoriteMapAsync(1, default)).ReturnsAsync((string?)null);
        _playerServiceMock.Setup(s => s.GetFavoriteWeaponAsync(1, default)).ReturnsAsync((FavoriteWeapon?)null);
        _playerServiceMock.Setup(s => s.GetKillStatsAsync(1, 0, default)).ReturnsAsync(Array.Empty<KillStatRow>());
        _playerServiceMock.Setup(s => s.GetMapPerformanceAsync(1, default)).ReturnsAsync(Array.Empty<MapStatRow>());
        _playerServiceMock.Setup(s => s.GetServerPerformanceAsync(1, default)).ReturnsAsync(Array.Empty<ServerStatRow>());
        _playerServiceMock.Setup(s => s.GetWeaponStatsAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<WeaponStatRow>());
        _playerServiceMock.Setup(s => s.GetWeaponStatsmeAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<WeaponStatsmeRow>());
        _playerServiceMock.Setup(s => s.GetWeaponTargetsAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<WeaponTargetRow>());
        _playerServiceMock.Setup(s => s.GetTeamSelectionAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<TeamStatRow>());
        _playerServiceMock.Setup(s => s.GetRoleSelectionAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<RoleStatRow>());
        _playerServiceMock.Setup(s => s.GetPlayerActionsAsync(1, default)).ReturnsAsync(Array.Empty<ActionStatRow>());
        _playerServiceMock.Setup(s => s.GetPlayerActionVictimsAsync(1, default)).ReturnsAsync(Array.Empty<ActionStatRow>());
        _playerServiceMock.Setup(s => s.GetTrendDataAsync(1, 30, default)).ReturnsAsync(Array.Empty<TrendPoint>());
        _playerServiceMock.Setup(s => s.GetGlobalAwardsAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<GlobalAwardRow>());
        _playerServiceMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);
        _awardServiceMock.Setup(s => s.GetRankForPlayerAsync(1, "cstrike", 500, default)).ReturnsAsync((Rank?)null);
        _awardServiceMock.Setup(s => s.GetRanksAsync("cstrike", default)).ReturnsAsync(Array.Empty<Rank>());

        var result = await _controller.Profile(1, ct: default);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PlayerProfileViewModel>().Subject;
        model.Player.LastName.Should().Be("FragMaster");
        model.Rank.Should().Be(1);
    }

    [Fact]
    public async Task Bans_ReturnsViewWithBannedPlayers()
    {
        var row   = new BanListRow(5, "Cheater", null, DateTime.UtcNow, 900, 10, 50, 30, 5, 1.67, 0.1, 25.0);
        var paged = PagedResult<BanListRow>.Create([row], 1, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("cstrike", 1, 50, "ban_date", true, 0, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans(null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<BanListViewModel>().Subject;
        model.Players.Items.Should().HaveCount(1);
        model.Players.Items[0].PlayerName.Should().Be("Cheater");
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        var paged = PagedResult<Player>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetLeaderboardAsync("dods", 1, 50, "skill", true, 1, default))
            .ReturnsAsync(paged);
        _playerServiceMock
            .Setup(s => s.GetHistoryDatesAsync("dods", default))
            .ReturnsAsync(Array.Empty<DateTime>());
        _awardServiceMock
            .Setup(s => s.GetRanksAsync("dods", default))
            .ReturnsAsync(Array.Empty<Rank>());

        var result = await _controller.Index("dods", 1, "skill", true);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerLeaderboardViewModel>()
            .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Bans_UsesDefaultGame_WhenGameNotProvided()
    {
        var paged = PagedResult<BanListRow>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("cstrike", 1, 50, "ban_date", true, 0, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans(null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<BanListViewModel>()
              .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Bans_UsesExplicitGame_WhenProvided()
    {
        var paged = PagedResult<BanListRow>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("dods", 1, 50, "ban_date", true, 0, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<BanListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Bans_PassesSortAndPageParametersThrough()
    {
        var paged = PagedResult<BanListRow>.Create([], 0, 2, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("cstrike", 2, 50, "kills", false, 0, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans(null, page: 2, sortBy: "kills", desc: false);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
                          .Should().BeOfType<BanListViewModel>().Subject;
        model.SortBy.Should().Be("kills");
        model.Descending.Should().BeFalse();
        model.Players.Page.Should().Be(2);
    }

    [Fact]
    public async Task Bans_PassesMinKillsParameterThrough()
    {
        var paged = PagedResult<BanListRow>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("cstrike", 1, 50, "ban_date", true, 100, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans(null, minKills: 100);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<BanListViewModel>()
              .Which.MinKills.Should().Be(100);
    }

    [Fact]
    public async Task Bans_ReturnsAllRowFields()
    {
        var banDate = new DateTime(2025, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var row     = new BanListRow(7, "WallHacker", "de", banDate, 1500, 75, 300, 120, 80, 2.5, 0.27, 42.0);
        var paged   = PagedResult<BanListRow>.Create([row], 1, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetBannedPlayersAsync("cstrike", 1, 50, "ban_date", true, 0, default))
            .ReturnsAsync(paged);

        var result = await _controller.Bans(null);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
                          .Should().BeOfType<BanListViewModel>().Subject;
        var item = model.Players.Items[0];
        item.PlayerId.Should().Be(7);
        item.PlayerName.Should().Be("WallHacker");
        item.Flag.Should().Be("de");
        item.BanDate.Should().Be(banDate);
        item.Skill.Should().Be(1500);
        item.ActivityScore.Should().Be(75);
        item.Kills.Should().Be(300);
        item.Deaths.Should().Be(120);
        item.Headshots.Should().Be(80);
        item.Kpd.Should().Be(2.5);
        item.HsK.Should().Be(0.27);
        item.Accuracy.Should().Be(42.0);
    }

    // ── Profile SourceTV / bot guards ────────────────────────────────────────

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenPlayerIsSourceTV()
    {
        var player = new Player { PlayerId = 2, LastName = "SourceTV", Game = "cstrike" };
        _playerServiceMock.Setup(s => s.GetPlayerAsync(2, default)).ReturnsAsync(player);

        var result = await _controller.Profile(2, ct: default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenPlayerIsBot()
    {
        var player = new Player
        {
            PlayerId = 3,
            LastName = "BOT_Fragginator",
            Game = "cstrike",
            UniqueIds = new List<PlayerUniqueId> { new() { UniqueId = "BOT_Fragginator" } }
        };
        _playerServiceMock.Setup(s => s.GetPlayerAsync(3, default)).ReturnsAsync(player);

        // Config already has HideBotPlayers absent → defaults to true in the controller
        var result = await _controller.Profile(3, ct: default);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── History ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task History_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playerServiceMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.History(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task History_ReturnsView_WhenPlayerExists()
    {
        var player = new Player { PlayerId = 1, LastName = "FragMaster", Game = "cstrike" };
        var events = PagedResult<PlayerEventRow>.Create([], 0, 1, 50);
        _playerServiceMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playerServiceMock
            .Setup(s => s.GetPlayerEventHistoryAsync(1, "cstrike", 1, 50, "eventTime", true, default))
            .ReturnsAsync(events);
        _playerServiceMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        var result = await _controller.History(1);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerEventHistoryViewModel>()
            .Which.Player.LastName.Should().Be("FragMaster");
    }

    // ── Sessions ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sessions_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playerServiceMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.Sessions(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Sessions_ReturnsView_WhenPlayerExists()
    {
        var player = new Player { PlayerId = 1, LastName = "FragMaster", Game = "cstrike" };
        var sessions = PagedResult<PlayerSessionRow>.Create([], 0, 1, 50);
        _playerServiceMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playerServiceMock
            .Setup(s => s.GetPlayerSessionsAsync(1, 1, 50, "eventTime", true, default))
            .ReturnsAsync(sessions);
        _playerServiceMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        var result = await _controller.Sessions(1);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerSessionsViewModel>()
            .Which.Player.LastName.Should().Be("FragMaster");
    }

    // ── Awards ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Awards_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playerServiceMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.Awards(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Awards_ReturnsView_Summary_WhenNoAwardIdProvided()
    {
        var player = new Player { PlayerId = 1, LastName = "FragMaster", Game = "cstrike" };
        var awards = PagedResult<PlayerAwardRow>.Create([], 0, 1, 50);
        _playerServiceMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playerServiceMock
            .Setup(s => s.GetPlayerAwardsSummaryAsync(1, 1, 50, "awardTime", true, default))
            .ReturnsAsync(awards);

        var result = await _controller.Awards(1);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerAwardsViewModel>()
            .Which.AwardId.Should().BeNull();
    }

    [Fact]
    public async Task Awards_ReturnsView_Detail_WhenAwardIdProvided()
    {
        var player = new Player { PlayerId = 1, LastName = "FragMaster", Game = "cstrike" };
        var awards = PagedResult<PlayerAwardRow>.Create([], 0, 1, 50);
        _playerServiceMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playerServiceMock
            .Setup(s => s.GetPlayerAwardDetailAsync(1, 42, 1, 50, "awardTime", true, default))
            .ReturnsAsync(awards);

        var result = await _controller.Awards(1, awardId: 42);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerAwardsViewModel>()
            .Which.AwardId.Should().Be(42);
    }

    // ── Index period leaderboard / minKills clamping ─────────────────────────

    [Fact]
    public async Task Index_UsesPeriodLeaderboard_WhenRankTypeIsWeek()
    {
        var periodResult = PagedResult<PlayerLeaderboardRow>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetPeriodLeaderboardAsync("cstrike",
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                1, 50, "skill", true, 1, default))
            .ReturnsAsync(periodResult);
        _playerServiceMock
            .Setup(s => s.GetHistoryDatesAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<DateTime>());
        _awardServiceMock
            .Setup(s => s.GetRanksAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<Rank>());

        var result = await _controller.Index(null, 1, "skill", true, rankType: "week");

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<PlayerLeaderboardViewModel>();
        _playerServiceMock.Verify(
            s => s.GetPeriodLeaderboardAsync("cstrike",
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                1, 50, "skill", true, 1, default),
            Times.Once);
        _playerServiceMock.Verify(
            s => s.GetLeaderboardAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_ClampsMinKillsToOne_WhenZero()
    {
        var paged = PagedResult<Player>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default))
            .ReturnsAsync(paged);
        _playerServiceMock
            .Setup(s => s.GetHistoryDatesAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<DateTime>());
        _awardServiceMock
            .Setup(s => s.GetRanksAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<Rank>());

        await _controller.Index(null, 1, "skill", true, minKills: 0);

        _playerServiceMock.Verify(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default), Times.Once);
    }

    [Fact]
    public async Task Index_ClampsMinKillsToOne_WhenNegative()
    {
        var paged = PagedResult<Player>.Create([], 0, 1, 50);
        _playerServiceMock
            .Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default))
            .ReturnsAsync(paged);
        _playerServiceMock
            .Setup(s => s.GetHistoryDatesAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<DateTime>());
        _awardServiceMock
            .Setup(s => s.GetRanksAsync("cstrike", default))
            .ReturnsAsync(Array.Empty<Rank>());

        await _controller.Index(null, 1, "skill", true, minKills: -5);

        _playerServiceMock.Verify(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default), Times.Once);
    }
}
