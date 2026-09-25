using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class MapsControllerTests
{
    private readonly Mock<IMapRepository>  _mapsMock;
    private readonly Mock<IPlayerService>  _playersMock;
    private readonly IConfiguration _config;
    private readonly MapsController _controller;

    public MapsControllerTests()
    {
        _mapsMock    = new Mock<IMapRepository>();
        _playersMock = new Mock<IPlayerService>();
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new MapsController(_mapsMock.Object, _playersMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithMapList()
    {
        var maps  = new List<MapCount> { new() { Map = "de_dust2", Kills = 3000 } };
        var paged = PagedResult<MapCount>.Create(maps, 1, 1, 50);

        _mapsMock.Setup(r => r.GetAllAsync("cstrike", 1, 50, "kills", true, default)).ReturnsAsync(paged);
        _mapsMock.Setup(r => r.GetKillTotalsAsync("cstrike", default)).ReturnsAsync((50000L, 8000L));

        var result = await _controller.Index(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<MapListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Maps.Items.Should().HaveCount(1);
        model.TotalKills.Should().Be(50000);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        _mapsMock.Setup(r => r.GetAllAsync("dods", 1, 50, "kills", true, default))
                 .ReturnsAsync(PagedResult<MapCount>.Create([], 0, 1, 50));
        _mapsMock.Setup(r => r.GetKillTotalsAsync("dods", default)).ReturnsAsync((0L, 0L));

        var result = await _controller.Index("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<MapListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsViewWithMapNameAndPlayers()
    {
        var players = PagedResult<MapPlayerRow>.Create(
            [new(1, "TopPlayer", null, 500, 50, 0.10)], 1, 1, 50);

        _mapsMock.Setup(r => r.GetMapTotalKillsAsync("de_dust2", "cstrike", default)).ReturnsAsync(3000L);
        _mapsMock.Setup(r => r.GetPlayerLeaderboardAsync("de_dust2", "cstrike", 1, 50, "kills", true, default))
                 .ReturnsAsync(players);

        var result = await _controller.Detail("de_dust2", "cstrike");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<MapDetailViewModel>().Subject;
        model.MapName.Should().Be("de_dust2");
        model.Game.Should().Be("cstrike");
        model.TotalKills.Should().Be(3000);
        model.Players.Items.Should().HaveCount(1);
        model.DeleteDays.Should().Be(90);
    }

    [Fact]
    public async Task Detail_DeleteDays_PropagatesFromService()
    {
        _mapsMock.Setup(r => r.GetMapTotalKillsAsync("de_inferno", "cstrike", default)).ReturnsAsync(0L);
        _mapsMock.Setup(r => r.GetPlayerLeaderboardAsync("de_inferno", "cstrike", 1, 50, "kills", true, default))
                 .ReturnsAsync(PagedResult<MapPlayerRow>.Create([], 0, 1, 50));
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(14);

        var result = await _controller.Detail("de_inferno", "cstrike");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<MapDetailViewModel>()
              .Which.DeleteDays.Should().Be(14);
    }

    [Fact]
    public async Task Detail_UsesDefaultGame_WhenGameIsNull()
    {
        _mapsMock.Setup(r => r.GetMapTotalKillsAsync("de_dust2", "cstrike", default)).ReturnsAsync(0L);
        _mapsMock.Setup(r => r.GetPlayerLeaderboardAsync("de_dust2", "cstrike", 1, 50, "kills", true, default))
                 .ReturnsAsync(PagedResult<MapPlayerRow>.Create([], 0, 1, 50));

        var result = await _controller.Detail("de_dust2", null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<MapDetailViewModel>()
              .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Detail_PassesSortAndPage_ToRepository()
    {
        _mapsMock.Setup(r => r.GetMapTotalKillsAsync("de_nuke", "cstrike", default)).ReturnsAsync(0L);
        _mapsMock.Setup(r => r.GetPlayerLeaderboardAsync("de_nuke", "cstrike", 2, 50, "headshots", false, default))
                 .ReturnsAsync(PagedResult<MapPlayerRow>.Create([], 0, 2, 50));

        await _controller.Detail("de_nuke", "cstrike", page: 2, sortBy: "headshots", desc: false);

        _mapsMock.Verify(r => r.GetPlayerLeaderboardAsync("de_nuke", "cstrike", 2, 50, "headshots", false, default), Times.Once);
    }
}
