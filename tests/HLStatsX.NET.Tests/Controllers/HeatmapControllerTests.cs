using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class HeatmapControllerTests
{
    private readonly Mock<IHeatmapRepository> _heatmapMock;
    private readonly IConfiguration _config;
    private readonly HeatmapController _controller;

    public HeatmapControllerTests()
    {
        _heatmapMock = new Mock<IHeatmapRepository>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"] = "cstrike"
            })
            .Build();

        _controller = new HeatmapController(_heatmapMock.Object, _config);
    }

    private static JsonResult AsJson(IActionResult result) =>
        result.Should().BeOfType<JsonResult>().Subject;

    private static dynamic Data(JsonResult result) => result.Value!;

    // ── Null / empty map ─────────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsError_WhenMapIsNull()
    {
        var result = AsJson(await _controller.Data("cstrike", null));

        ((object)result.Value!).GetType().GetProperty("error").Should().NotBeNull();
    }

    [Fact]
    public async Task Data_ReturnsError_WhenMapIsEmpty()
    {
        var result = AsJson(await _controller.Data("cstrike", ""));

        ((object)result.Value!).GetType().GetProperty("error").Should().NotBeNull();
    }

    // ── Type = kills ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsKillsAndEmptyDeaths_WhenTypeIsKills()
    {
        var killPoints = new List<HeatPoint> { new(100.0, 200.0, 5) };
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ReturnsAsync((HeatmapConfig?)null);
        _heatmapMock.Setup(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, null, default))
                    .ReturnsAsync(killPoints);

        var result = AsJson(await _controller.Data("cstrike", "de_dust2", type: "kills"));

        var value = result.Value!;
        var kills  = (IEnumerable<object>)value.GetType().GetProperty("kills")!.GetValue(value)!;
        var deaths = (IEnumerable<object>)value.GetType().GetProperty("deaths")!.GetValue(value)!;

        kills.Should().HaveCount(1);
        deaths.Should().BeEmpty();

        _heatmapMock.Verify(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, null, default), Times.Once);
        _heatmapMock.Verify(r => r.GetDeathPointsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Type = deaths ────────────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsDeathsAndEmptyKills_WhenTypeIsDeaths()
    {
        var deathPoints = new List<HeatPoint> { new(50.0, 75.0, 3) };
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ReturnsAsync((HeatmapConfig?)null);
        _heatmapMock.Setup(r => r.GetDeathPointsAsync("cstrike", "de_dust2", 0, 64, null, default))
                    .ReturnsAsync(deathPoints);

        var result = AsJson(await _controller.Data("cstrike", "de_dust2", type: "deaths"));

        var value  = result.Value!;
        var kills  = (IEnumerable<object>)value.GetType().GetProperty("kills")!.GetValue(value)!;
        var deaths = (IEnumerable<object>)value.GetType().GetProperty("deaths")!.GetValue(value)!;

        kills.Should().BeEmpty();
        deaths.Should().HaveCount(1);

        _heatmapMock.Verify(r => r.GetKillPointsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        _heatmapMock.Verify(r => r.GetDeathPointsAsync("cstrike", "de_dust2", 0, 64, null, default), Times.Once);
    }

    // ── Type = both ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsBothKillsAndDeaths_WhenTypeIsBoth()
    {
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ReturnsAsync((HeatmapConfig?)null);
        _heatmapMock.Setup(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, null, default))
                    .ReturnsAsync([new(10.0, 20.0, 2)]);
        _heatmapMock.Setup(r => r.GetDeathPointsAsync("cstrike", "de_dust2", 0, 64, null, default))
                    .ReturnsAsync([new(30.0, 40.0, 1)]);

        var result = AsJson(await _controller.Data("cstrike", "de_dust2", type: "both"));

        var value  = result.Value!;
        var kills  = (IEnumerable<object>)value.GetType().GetProperty("kills")!.GetValue(value)!;
        var deaths = (IEnumerable<object>)value.GetType().GetProperty("deaths")!.GetValue(value)!;

        kills.Should().HaveCount(1);
        deaths.Should().HaveCount(1);
    }

    // ── Config ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsNullConfig_WhenNoConfigFound()
    {
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ReturnsAsync((HeatmapConfig?)null);
        _heatmapMock.Setup(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, null, default))
                    .ReturnsAsync([]);

        var result = AsJson(await _controller.Data("cstrike", "de_dust2"));

        var value  = result.Value!;
        var config = value.GetType().GetProperty("config")!.GetValue(value);

        config.Should().BeNull();
    }

    [Fact]
    public async Task Data_ReturnsConfigShape_WhenConfigFound()
    {
        var cfg = new HeatmapConfig
        {
            Map = "de_dust2", Game = "cstrike",
            XOffset = 10f, YOffset = 20f,
            FlipX = true, FlipY = false, Scale = 1.5f, Days = 7
        };
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default)).ReturnsAsync(cfg);
        _heatmapMock.Setup(r => r.GetKillPointsAsync("cstrike", "de_dust2", 7, 64, null, default))
                    .ReturnsAsync([]);

        var result = AsJson(await _controller.Data("cstrike", "de_dust2"));

        var value   = result.Value!;
        var cfgVal  = value.GetType().GetProperty("config")!.GetValue(value);

        cfgVal.Should().NotBeNull();
        var xoffset = cfgVal!.GetType().GetProperty("xoffset")!.GetValue(cfgVal);
        xoffset.Should().Be(10f);
    }

    // ── Exception handling ───────────────────────────────────────────────────

    [Fact]
    public async Task Data_ReturnsErrorJson_WhenExceptionThrown()
    {
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var result = AsJson(await _controller.Data("cstrike", "de_dust2"));

        var value = result.Value!;
        var error = value.GetType().GetProperty("error")!.GetValue(value) as string;

        error.Should().Contain("DB unavailable");
    }

    // ── PlayerId passthrough ─────────────────────────────────────────────────

    [Fact]
    public async Task Data_PassesPlayerId_ToRepository()
    {
        _heatmapMock.Setup(r => r.GetConfigAsync("cstrike", "de_dust2", default))
                    .ReturnsAsync((HeatmapConfig?)null);
        _heatmapMock.Setup(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, 42, default))
                    .ReturnsAsync([]);

        await _controller.Data("cstrike", "de_dust2", type: "kills", playerId: 42);

        _heatmapMock.Verify(r => r.GetKillPointsAsync("cstrike", "de_dust2", 0, 64, 42, default), Times.Once);
    }
}
