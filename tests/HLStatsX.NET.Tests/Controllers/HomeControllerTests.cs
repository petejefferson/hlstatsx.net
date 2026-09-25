using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IPlayerService> _playersMock;
    private readonly Mock<IServerService> _serversMock;
    private readonly Mock<IAwardService>  _awardsMock;
    private readonly Mock<IGameService>   _gameMock;
    private readonly Mock<IAdminService>  _adminMock;
    private readonly IConfiguration _config;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _playersMock = new Mock<IPlayerService>();
        _serversMock = new Mock<IServerService>();
        _awardsMock  = new Mock<IAwardService>();
        _gameMock    = new Mock<IGameService>();
        _adminMock   = new Mock<IAdminService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"] = "cstrike"
            })
            .Build();

        _adminMock.Setup(s => s.GetOptionsAsync(default))
                  .ReturnsAsync(new Dictionary<string, string>());

        _controller = new HomeController(
            _playersMock.Object, _serversMock.Object, _awardsMock.Object,
            _gameMock.Object, _adminMock.Object, _config);
    }

    private void SetupDefaultMocks(string game = "cstrike", int playerCount = 500, int trend24h = 400)
    {
        var gameStats = new GameStats(100000, 20000, 2, trend24h, -1);

        _serversMock.Setup(s => s.GetServersAsync(game, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetGameStatsAsync(game, default)).ReturnsAsync(gameStats);
        _serversMock.Setup(s => s.GetAllLivestatsAsync(game, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetServerLoadAsync(game, 24, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetTrendSeriesAsync(game, 24, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetTeamsAsync(game, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetVoiceCommServersAsync(default)).ReturnsAsync([]);
        _playersMock.Setup(s => s.GetTotalCountAsync(game, default)).ReturnsAsync(playerCount);
        _awardsMock.Setup(s => s.GetDailyAwardsAsync(game, default)).ReturnsAsync([]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithHomeViewModel()
    {
        SetupDefaultMocks();

        var result = await _controller.Index("cstrike", default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<HomeViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.TotalPlayers.Should().Be(500);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        SetupDefaultMocks("dods", 100, -1);

        var result = await _controller.Index("dods", default);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<HomeViewModel>()
              .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Index_CalculatesNewPlayers24h_WhenTrendDataAvailable()
    {
        // 500 current players, trend snapshot was 400 → 100 new players
        SetupDefaultMocks(playerCount: 500, trend24h: 400);

        var result = await _controller.Index("cstrike", default);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<HomeViewModel>()
              .Which.NewPlayersLast24h.Should().Be(100);
    }

    [Fact]
    public async Task Index_ReturnsMinusOne_WhenNoTrendData()
    {
        SetupDefaultMocks(playerCount: 500, trend24h: -1);

        var result = await _controller.Index("cstrike", default);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<HomeViewModel>()
              .Which.NewPlayersLast24h.Should().Be(-1);
    }

    [Fact]
    public async Task Index_RedirectsToServerDetail_WhenExactlyOneServerConfigured()
    {
        var game = "cstrike";
        var server = new Server { ServerId = 7, Name = "Solo", Game = game };
        var gameStats = new GameStats(50000, 10000, 1, -1, -1);

        _serversMock.Setup(s => s.GetServersAsync(game, default)).ReturnsAsync([server]);
        _serversMock.Setup(s => s.GetGameStatsAsync(game, default)).ReturnsAsync(gameStats);

        var result = await _controller.Index("cstrike", default);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Detail");
        redirect.ControllerName.Should().Be("Servers");
        redirect.RouteValues!["id"].Should().Be(7);
    }

    [Fact]
    public async Task Index_ShowsGamesList_WhenNoGameAndMultipleGames()
    {
        var games = new List<GameListRow>
        {
            new("cstrike", "Counter-Strike", 0, 0, null, null, null, null),
            new("dods",    "Day of Defeat: Source", 0, 0, null, null, null, null),
        };
        var data = new GamesListData(games, 1000, 50, 3, 500000L, null, 90);
        _gameMock.Setup(s => s.GetGamesListAsync(default)).ReturnsAsync(data);

        var result = await _controller.Index(null, default);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("GamesList");
        var model = view.Model.Should().BeOfType<GamesListData>().Subject;
        model.Games.Should().HaveCount(2);
        model.DeleteDays.Should().Be(90);
    }

    [Fact]
    public async Task Index_RedirectsToSingleGame_WhenGamesListHasOneEntry()
    {
        var games = new List<GameListRow>
        {
            new("dods", "Day of Defeat: Source", 0, 0, null, null, null, null)
        };
        var data = new GamesListData(games, 100, 10, 1, 10000L, null, 90);
        _gameMock.Setup(s => s.GetGamesListAsync(default)).ReturnsAsync(data);
        SetupDefaultMocks("dods");

        var result = await _controller.Index(null, default);

        // Should proceed as if game="dods" was provided — returns HomeViewModel, not GamesList
        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<HomeViewModel>();
    }

    [Fact]
    public async Task Error_ReturnsView()
    {
        var result = _controller.Error();

        result.Should().BeOfType<ViewResult>();
    }
}
