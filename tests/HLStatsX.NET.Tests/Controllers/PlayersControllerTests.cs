using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
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

        _controller = new PlayersController(_playerServiceMock.Object, _awardServiceMock.Object, _config);
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
            .Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, default))
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

        var result = await _controller.Profile(999, default);

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
        _playerServiceMock.Setup(s => s.GetKillStatsAsync(1, default)).ReturnsAsync(Array.Empty<KillStatRow>());
        _playerServiceMock.Setup(s => s.GetMapPerformanceAsync(1, default)).ReturnsAsync(Array.Empty<MapStatRow>());
        _playerServiceMock.Setup(s => s.GetServerPerformanceAsync(1, default)).ReturnsAsync(Array.Empty<ServerStatRow>());
        _playerServiceMock.Setup(s => s.GetWeaponStatsAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<WeaponStatRow>());
        _playerServiceMock.Setup(s => s.GetTeamSelectionAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<TeamStatRow>());
        _playerServiceMock.Setup(s => s.GetRoleSelectionAsync(1, "cstrike", default)).ReturnsAsync(Array.Empty<RoleStatRow>());
        _playerServiceMock.Setup(s => s.GetPlayerActionsAsync(1, default)).ReturnsAsync(Array.Empty<ActionStatRow>());
        _playerServiceMock.Setup(s => s.GetPlayerActionVictimsAsync(1, default)).ReturnsAsync(Array.Empty<ActionStatRow>());
        _awardServiceMock.Setup(s => s.GetRankForPlayerAsync(1, "cstrike", 500, default)).ReturnsAsync((Rank?)null);
        _awardServiceMock.Setup(s => s.GetRanksAsync("cstrike", default)).ReturnsAsync(Array.Empty<Rank>());

        var result = await _controller.Profile(1, default);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PlayerProfileViewModel>().Subject;
        model.Player.LastName.Should().Be("FragMaster");
        model.Rank.Should().Be(1);
    }

    [Fact]
    public async Task Bans_ReturnsViewWithBannedPlayers()
    {
        var banned = new List<Player>
        {
            new() { PlayerId = 5, LastName = "Cheater", HideRanking = 1, Game = "cstrike" }
        };
        _playerServiceMock.Setup(s => s.GetBannedPlayersAsync("cstrike", default)).ReturnsAsync(banned);

        var result = await _controller.Bans(null, default);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<BanListViewModel>().Subject;
        model.BannedPlayers.Should().HaveCount(1);
    }
}
