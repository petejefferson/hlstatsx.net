using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class ServersControllerTests
{
    private readonly Mock<IServerService> _serversMock;
    private readonly Mock<IPlayerService> _playersMock;
    private readonly Mock<IAwardService> _awardsMock;
    private readonly Mock<IAdminService> _adminMock;
    private readonly IConfiguration _config;
    private readonly ServersController _controller;

    public ServersControllerTests()
    {
        _serversMock = new Mock<IServerService>();
        _playersMock = new Mock<IPlayerService>();
        _awardsMock  = new Mock<IAwardService>();
        _adminMock   = new Mock<IAdminService>();

        _adminMock.Setup(s => s.GetOptionsAsync(default))
                  .ReturnsAsync(new Dictionary<string, string>());

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"] = "cstrike"
            })
            .Build();

        _controller = new ServersController(_serversMock.Object, _playersMock.Object, _awardsMock.Object, _adminMock.Object, _config);
    }

    private void SetupIndexMocks(string game, int playerCount, int trend24h, IReadOnlyList<Server>? servers = null)
    {
        servers ??= [];
        var gameStats = new GameStats(50000, 10000, servers.Count, trend24h, -1);

        _serversMock.Setup(s => s.GetServersAsync(game, default)).ReturnsAsync(servers);
        _serversMock.Setup(s => s.GetGameStatsAsync(game, default)).ReturnsAsync(gameStats);
        _serversMock.Setup(s => s.GetTrendSeriesAsync(game, 24, default)).ReturnsAsync([]);
        _playersMock.Setup(s => s.GetTotalCountAsync(game, default)).ReturnsAsync(playerCount);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithServerList()
    {
        var servers = new List<Server>
        {
            new() { ServerId = 1, Name = "FragServer",  Game = "cstrike" },
            new() { ServerId = 2, Name = "FragServer2", Game = "cstrike" }
        };
        SetupIndexMocks("cstrike", 200, 150, servers);

        var result = await _controller.Index(null, default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ServerListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Servers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_CalculatesNewPlayers24h_WhenTrendDataAvailable()
    {
        SetupIndexMocks("cstrike", 200, 150);

        var result = await _controller.Index(null, default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                         .Model.Should().BeOfType<ServerListViewModel>().Subject;
        // 200 current - 150 trend snapshot = 50 new players
        model.NewPlayersLast24h.Should().Be(50);
    }

    [Fact]
    public async Task Index_ReturnsMinusOne_WhenTrendDataUnavailable()
    {
        SetupIndexMocks("cstrike", 200, -1); // -1 means no trend data

        var result = await _controller.Index(null, default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                         .Model.Should().BeOfType<ServerListViewModel>().Subject;
        model.NewPlayersLast24h.Should().Be(-1);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        SetupIndexMocks("dods", 50, -1);

        var result = await _controller.Index("dods", default);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<ServerListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenServerDoesNotExist()
    {
        _serversMock.Setup(s => s.GetServerAsync(99, default)).ReturnsAsync((Server?)null);

        var result = await _controller.Detail(99, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Detail_ReturnsView_WithServerAndLivestats()
    {
        var server = new Server { ServerId = 1, Name = "FragServer", Game = "cstrike" };
        var live   = new List<Livestat> { new() { PlayerId = 5, Name = "FragLord" } };
        var gameStats = new GameStats(50000, 10000, 1, -1, -1);

        _serversMock.Setup(s => s.GetServerAsync(1, default)).ReturnsAsync(server);
        _serversMock.Setup(s => s.GetLivestatsAsync(1, default)).ReturnsAsync(live);
        _serversMock.Setup(s => s.GetServerLoadByServerIdAsync(1, 24, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetGameStatsAsync("cstrike", default)).ReturnsAsync(gameStats);
        _serversMock.Setup(s => s.GetTrendSeriesAsync("cstrike", 24, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetTeamsAsync("cstrike", default)).ReturnsAsync([]);
        _playersMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(200);
        _awardsMock.Setup(s => s.GetDailyAwardsAsync("cstrike", default)).ReturnsAsync([]);

        var result = await _controller.Detail(1, default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ServerDetailViewModel>().Subject;
        model.Server.Name.Should().Be("FragServer");
        model.Livestats.Should().HaveCount(1);
    }

    // ── Livestats ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Livestats_ReturnsNotFound_WhenServerDoesNotExist()
    {
        _serversMock.Setup(s => s.GetServerAsync(99, default)).ReturnsAsync((Server?)null);

        var result = await _controller.Livestats(99, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Livestats_ReturnsView_WithLivestatData()
    {
        var server = new Server { ServerId = 3, Name = "LiveServer", Game = "dods" };
        var live   = new List<Livestat> { new() { PlayerId = 7, Name = "SnipeKing", Team = "allies" } };

        _serversMock.Setup(s => s.GetServerAsync(3, default)).ReturnsAsync(server);
        _serversMock.Setup(s => s.GetLivestatsAsync(3, default)).ReturnsAsync(live);
        _serversMock.Setup(s => s.GetTeamsAsync("dods", default)).ReturnsAsync([]);

        var result = await _controller.Livestats(3, default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ServerLivestatsViewModel>().Subject;
        model.Server.Name.Should().Be("LiveServer");
        model.Game.Should().Be("dods");
        model.Livestats.Should().HaveCount(1);
        model.Livestats[0].Name.Should().Be("SnipeKing");
    }

    [Fact]
    public async Task Livestats_PopulatesTeamDictionary()
    {
        var server = new Server { ServerId = 1, Name = "FragServer", Game = "cstrike" };
        var teams  = new List<Team>
        {
            new() { Code = "ct",  Name = "Counter-Terrorists", PlayerlistIndex = 1 },
            new() { Code = "t",   Name = "Terrorists",         PlayerlistIndex = 2 }
        };

        _serversMock.Setup(s => s.GetServerAsync(1, default)).ReturnsAsync(server);
        _serversMock.Setup(s => s.GetLivestatsAsync(1, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetTeamsAsync("cstrike", default)).ReturnsAsync(teams);

        var result = await _controller.Livestats(1, default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<ServerLivestatsViewModel>().Subject;
        model.Teams.Should().ContainKey("ct");
        model.Teams.Should().ContainKey("t");
        model.GetTeam("ct")!.Name.Should().Be("Counter-Terrorists");
    }

    [Fact]
    public async Task Livestats_UsesGameFromServer_NotConfig()
    {
        var server = new Server { ServerId = 5, Name = "TF2 Server", Game = "tf" };

        _serversMock.Setup(s => s.GetServerAsync(5, default)).ReturnsAsync(server);
        _serversMock.Setup(s => s.GetLivestatsAsync(5, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetTeamsAsync("tf", default)).ReturnsAsync([]);

        var result = await _controller.Livestats(5, default);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<ServerLivestatsViewModel>().Subject;
        model.Game.Should().Be("tf");
        _serversMock.Verify(s => s.GetTeamsAsync("tf", default), Times.Once);
        _serversMock.Verify(s => s.GetTeamsAsync("cstrike", default), Times.Never);
    }

    // ── LoadChart ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadChart_DefaultRange_ReturnsJsonResult()
    {
        var rows = new List<HLStatsX.NET.Core.Entities.ServerLoad>
        {
            new() { ServerId = 1, Timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ActPlayers = 5, MaxPlayers = 20, Map = "de_dust2" }
        };
        _serversMock.Setup(s => s.GetServerLoadRangedAsync(1, 1, default)).ReturnsAsync(rows);

        var result = await _controller.LoadChart(1, 1);

        result.Should().BeOfType<JsonResult>();
        _serversMock.Verify(s => s.GetServerLoadRangedAsync(1, 1, default), Times.Once);
    }

    [Fact]
    public async Task LoadChart_ExplicitRange2_CallsServiceWithRange2()
    {
        _serversMock.Setup(s => s.GetServerLoadRangedAsync(1, 2, default)).ReturnsAsync([]);

        var result = await _controller.LoadChart(1, 2);

        result.Should().BeOfType<JsonResult>();
        _serversMock.Verify(s => s.GetServerLoadRangedAsync(1, 2, default), Times.Once);
    }

    [Fact]
    public async Task LoadChart_InvalidRange_IsClampedToOne()
    {
        _serversMock.Setup(s => s.GetServerLoadRangedAsync(1, 1, default)).ReturnsAsync([]);

        var result = await _controller.LoadChart(1, 99);

        result.Should().BeOfType<JsonResult>();
        _serversMock.Verify(s => s.GetServerLoadRangedAsync(1, 1, default), Times.Once);
        _serversMock.Verify(s => s.GetServerLoadRangedAsync(1, 99, default), Times.Never);
    }

    [Fact]
    public async Task LoadChart_EmptyData_ReturnsEmptyJsonArray()
    {
        _serversMock.Setup(s => s.GetServerLoadRangedAsync(2, 1, default)).ReturnsAsync([]);

        var result = await _controller.LoadChart(2);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        var serialized = System.Text.Json.JsonSerializer.Serialize(json.Value);
        serialized.Should().Be("[]");
    }

    [Fact]
    public async Task LoadChart_JsonData_ContainsExpectedFields()
    {
        var timestamp = (int)new DateTimeOffset(2024, 3, 15, 14, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var rows = new List<HLStatsX.NET.Core.Entities.ServerLoad>
        {
            new() { ServerId = 1, Timestamp = timestamp, ActPlayers = 7, MaxPlayers = 16, Map = "de_inferno" }
        };
        _serversMock.Setup(s => s.GetServerLoadRangedAsync(1, 1, default)).ReturnsAsync(rows);

        var result = await _controller.LoadChart(1, 1);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        var serialized = System.Text.Json.JsonSerializer.Serialize(json.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(serialized);
        var first = doc.RootElement[0];
        first.GetProperty("actPlayers").GetInt32().Should().Be(7);
        first.GetProperty("maxPlayers").GetInt32().Should().Be(16);
        first.GetProperty("map").GetString().Should().Be("de_inferno");
    }
}
