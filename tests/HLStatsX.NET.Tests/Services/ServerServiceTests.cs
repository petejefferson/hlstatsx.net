using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Tests.Services;

public class ServerServiceTests
{
    private readonly Mock<IServerRepository> _repoMock;
    private readonly IMemoryCache _cache;
    private readonly ServerService _service;

    public ServerServiceTests()
    {
        _repoMock = new Mock<IServerRepository>();
        _cache    = new MemoryCache(new MemoryCacheOptions());
        _service  = new ServerService(_repoMock.Object, _cache);
    }

    // ── GetTeamsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeams_OnFirstCall()
    {
        var teams = new List<Team>
        {
            new() { Code = "CT", Name = "Counter-Terrorists" },
            new() { Code = "T",  Name = "Terrorists" },
        };
        _repoMock.Setup(r => r.GetTeamsAsync("cstrike", default)).ReturnsAsync(teams);

        var result = await _service.GetTeamsAsync("cstrike");

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetTeamsAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsCachedValue_OnSecondCall()
    {
        _repoMock.Setup(r => r.GetTeamsAsync("cstrike", default))
                 .ReturnsAsync([new() { Code = "CT", Name = "Counter-Terrorists" }]);

        await _service.GetTeamsAsync("cstrike");
        await _service.GetTeamsAsync("cstrike");

        _repoMock.Verify(r => r.GetTeamsAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetTeamsAsync_CachesPerGame()
    {
        _repoMock.Setup(r => r.GetTeamsAsync("cstrike", default)).ReturnsAsync([new() { Code = "CT" }]);
        _repoMock.Setup(r => r.GetTeamsAsync("dods",    default)).ReturnsAsync([new() { Code = "Allies" }]);

        await _service.GetTeamsAsync("cstrike");
        await _service.GetTeamsAsync("dods");

        _repoMock.Verify(r => r.GetTeamsAsync("cstrike", default), Times.Once);
        _repoMock.Verify(r => r.GetTeamsAsync("dods",    default), Times.Once);
    }

    // ── CreateServerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateServerAsync_ReturnsPassedServer()
    {
        var server = new Server { Name = "My Server", Game = "cstrike" };
        _repoMock.Setup(r => r.AddAsync(server, default)).Returns(Task.CompletedTask);

        var result = await _service.CreateServerAsync(server);

        result.Should().BeSameAs(server);
        _repoMock.Verify(r => r.AddAsync(server, default), Times.Once);
    }

    // ── DeleteServerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteServerAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DeleteAsync(7, default)).Returns(Task.CompletedTask);

        await _service.DeleteServerAsync(7);

        _repoMock.Verify(r => r.DeleteAsync(7, default), Times.Once);
    }

    // ── UpdateServerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateServerAsync_DelegatesToRepository()
    {
        var server = new Server { ServerId = 3, Name = "Updated" };
        _repoMock.Setup(r => r.UpdateAsync(server, default)).Returns(Task.CompletedTask);

        await _service.UpdateServerAsync(server);

        _repoMock.Verify(r => r.UpdateAsync(server, default), Times.Once);
    }

    // ── GetGameStatsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetGameStatsAsync_DelegatesToRepository()
    {
        var stats = new GameStats(TotalKills: 5000, TotalHeadshots: 2500, TotalServers: 3, Trend24hPlayers: 100, Trend24hKills: 4800);
        _repoMock.Setup(r => r.GetGameStatsAsync("cstrike", default)).ReturnsAsync(stats);

        var result = await _service.GetGameStatsAsync("cstrike");

        result.TotalKills.Should().Be(5000);
        _repoMock.Verify(r => r.GetGameStatsAsync("cstrike", default), Times.Once);
    }

    // ── GetServerAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetServerAsync_ReturnsServer_WhenFound()
    {
        var server = new Server { ServerId = 2, Name = "MyServer" };
        _repoMock.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(server);

        var result = await _service.GetServerAsync(2);

        result.Should().NotBeNull();
        result!.Name.Should().Be("MyServer");
    }

    [Fact]
    public async Task GetServerAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Server?)null);

        var result = await _service.GetServerAsync(99);

        result.Should().BeNull();
    }

    // ── GetServersAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetServersAsync_ReturnsActiveServers()
    {
        var servers = new List<Server>
        {
            new() { ServerId = 1, Name = "Server A", Game = "cstrike" },
            new() { ServerId = 2, Name = "Server B", Game = "cstrike" },
        };
        _repoMock.Setup(r => r.GetActiveAsync("cstrike", default)).ReturnsAsync(servers);

        var result = await _service.GetServersAsync("cstrike");

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetActiveAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetServersAsync_WithNullGame_PassesNullToRepository()
    {
        _repoMock.Setup(r => r.GetActiveAsync(null, default)).ReturnsAsync(Array.Empty<Server>());

        await _service.GetServersAsync(null);

        _repoMock.Verify(r => r.GetActiveAsync(null, default), Times.Once);
    }

    // ── GetLivestatsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetLivestatsAsync_DelegatesToRepository()
    {
        var stats = new List<Livestat> { new() { PlayerId = 1, ServerId = 5 } };
        _repoMock.Setup(r => r.GetLivestatsAsync(5, default)).ReturnsAsync(stats);

        var result = await _service.GetLivestatsAsync(5);

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetLivestatsAsync(5, default), Times.Once);
    }

    // ── GetServerLoadRangedAsync / Downsample ─────────────────────────────────

    [Fact]
    public async Task GetServerLoadRangedAsync_Range1_Uses24hRepository()
    {
        _repoMock.Setup(r => r.GetServerLoadByServerIdAsync(1, 24, default)).ReturnsAsync([]);

        await _service.GetServerLoadRangedAsync(1, 1);

        _repoMock.Verify(r => r.GetServerLoadByServerIdAsync(1, 24, default), Times.Once);
        _repoMock.Verify(r => r.GetServerLoadAllByServerIdAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_Range2_UsesAllRowsRepository()
    {
        _repoMock.Setup(r => r.GetServerLoadAllByServerIdAsync(1, default)).ReturnsAsync([]);

        await _service.GetServerLoadRangedAsync(1, 2);

        _repoMock.Verify(r => r.GetServerLoadAllByServerIdAsync(1, default), Times.Once);
        _repoMock.Verify(r => r.GetServerLoadByServerIdAsync(It.IsAny<int>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_Range1_ReturnsRowsUnchanged()
    {
        var rows = new List<ServerLoad>
        {
            new() { ServerId = 1, Timestamp = 100, ActPlayers = 5, MinPlayers = 2, MaxPlayers = 10 },
            new() { ServerId = 1, Timestamp = 200, ActPlayers = 8, MinPlayers = 3, MaxPlayers = 12 },
        };
        _repoMock.Setup(r => r.GetServerLoadByServerIdAsync(1, 24, default)).ReturnsAsync(rows);

        var result = await _service.GetServerLoadRangedAsync(1, 1);

        result.Should().HaveCount(2);
        result[0].ActPlayers.Should().Be(5);
        result[1].ActPlayers.Should().Be(8);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_Range2_AveragesActPlayersInChunksOf7()
    {
        // 14 rows → 2 chunks of 7; avg of 1..7 = 4, avg of 8..14 = 11
        var rows = Enumerable.Range(1, 14).Select(i => new ServerLoad
        {
            ServerId = 1, Timestamp = 1000 + i * 60,
            ActPlayers = i, MinPlayers = 0, MaxPlayers = 20
        }).ToList();
        _repoMock.Setup(r => r.GetServerLoadAllByServerIdAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetServerLoadRangedAsync(1, 2);

        result.Should().HaveCount(2);
        result[0].ActPlayers.Should().Be(4);
        result[1].ActPlayers.Should().Be(11);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_Range2_TimestampFromMidpointElement()
    {
        // avgStep=7 → mid = ceil(7/2)-1 = 3 (0-indexed)
        // chunk 1 start=0 → rows[3].Timestamp; chunk 2 start=7 → rows[10].Timestamp
        var rows = Enumerable.Range(0, 14).Select(i => new ServerLoad
        {
            ServerId = 1, Timestamp = 1000 + i * 60,
            ActPlayers = 1, MinPlayers = 0, MaxPlayers = 10
        }).ToList();
        _repoMock.Setup(r => r.GetServerLoadAllByServerIdAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetServerLoadRangedAsync(1, 2);

        result[0].Timestamp.Should().Be(rows[3].Timestamp);
        result[1].Timestamp.Should().Be(rows[10].Timestamp);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_Range2_PartialLastChunk_ClampsMidIndex()
    {
        // 10 rows: chunk1 = [0..6] (full), chunk2 = [7..9] (3 rows, partial)
        // partial chunk: mid clamped to count-1 = 2 → timestamp = rows[7+2] = rows[9]
        var rows = Enumerable.Range(0, 10).Select(i => new ServerLoad
        {
            ServerId = 1, Timestamp = 1000 + i * 60,
            ActPlayers = 1, MinPlayers = 0, MaxPlayers = 10
        }).ToList();
        _repoMock.Setup(r => r.GetServerLoadAllByServerIdAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetServerLoadRangedAsync(1, 2);

        result.Should().HaveCount(2);
        result[1].Timestamp.Should().Be(rows[9].Timestamp);
    }

    [Fact]
    public async Task GetServerLoadRangedAsync_EmptyRows_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetServerLoadAllByServerIdAsync(1, default)).ReturnsAsync([]);

        var result = await _service.GetServerLoadRangedAsync(1, 3);

        result.Should().BeEmpty();
    }
}
