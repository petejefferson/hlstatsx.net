using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.Network;
using HLStatsX.NET.Daemon.Query;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Daemon.Events;

/// <summary>
/// Tests for <see cref="MapChangeHandler"/>.
/// DB calls are blocked by a throwing mock factory, which lets us verify in-memory
/// state is updated before any I/O occurs.
/// </summary>
public sealed class MapChangeHandlerTests
{
    // ── Loading map — CurrentMap updated immediately ──────────────────────────

    [Fact]
    public async Task HandleAsync_Loading_SetsCurrentMap()
    {
        var (handler, _) = BuildHandler();
        var server = new ServerState();

        await handler.HandleAsync("loading", "dod_anzio", server, nowUnix: 0, isReplay: true);

        server.CurrentMap.Should().Be("dod_anzio");
    }

    [Fact]
    public async Task HandleAsync_Loading_ReplacesPreviousMap()
    {
        var (handler, _) = BuildHandler();
        var server = new ServerState { CurrentMap = "dod_flash" };

        await handler.HandleAsync("loading", "dod_anzio", server, nowUnix: 0, isReplay: true);

        server.CurrentMap.Should().Be("dod_anzio");
    }

    [Fact]
    public async Task HandleAsync_Loading_NoPlayersInServer_CompletesWithoutDbAccess()
    {
        var (handler, dbFactory) = BuildHandler();
        var server = new ServerState();

        await handler.HandleAsync("loading", "dod_anzio", server, nowUnix: 0, isReplay: true);

        // No DB operations expected for loading with zero players
        dbFactory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Loading_ResetsKillStreaksForExistingPlayers()
    {
        var (handler, _) = BuildHandler();
        var server = new ServerState();
        var player = new PlayerSession { UserId = 1, UniqueId = "STEAM_0:1:1", KillsThisLife = 5 };
        server.AddPlayer(player);

        await handler.HandleAsync("loading", "dod_anzio", server, nowUnix: 0, isReplay: true);

        player.KillsThisLife.Should().Be(0);
    }

    // ── Started map — CurrentMap also updated ────────────────────────────────

    [Fact]
    public async Task HandleAsync_Started_SetsCurrentMapBeforeDbAccess()
    {
        // DB factory throws so we can verify the in-memory update happened first
        var (handler, dbFactory) = BuildHandler();
        dbFactory.Setup(f => f.CreateDbContext())
                 .Throws(new InvalidOperationException("no DB in unit tests"));
        var server = new ServerState();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("started", "dod_anzio", server, nowUnix: 0, isReplay: true));

        server.CurrentMap.Should().Be("dod_anzio");
    }

    [Fact]
    public async Task HandleAsync_Started_ReplacesPreviousMap()
    {
        var (handler, dbFactory) = BuildHandler();
        dbFactory.Setup(f => f.CreateDbContext())
                 .Throws(new InvalidOperationException("no DB in unit tests"));
        var server = new ServerState { CurrentMap = "dod_flash" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("started", "dod_anzio", server, nowUnix: 0, isReplay: true));

        server.CurrentMap.Should().Be("dod_anzio");
    }

    [Fact]
    public async Task HandleAsync_Started_SetsBonusRoundFalse()
    {
        var (handler, dbFactory) = BuildHandler();
        dbFactory.Setup(f => f.CreateDbContext())
                 .Throws(new InvalidOperationException("no DB in unit tests"));
        var server = new ServerState { InBonusRound = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("started", "dod_anzio", server, nowUnix: 0, isReplay: true));

        server.InBonusRound.Should().BeFalse();
    }

    // ── Both types use the same map name ─────────────────────────────────────

    [Theory]
    [InlineData("loading")]
    [InlineData("started")]
    public async Task HandleAsync_BothTypes_SetCurrentMap(string type)
    {
        var (handler, dbFactory) = BuildHandler();
        if (type == "started")
            dbFactory.Setup(f => f.CreateDbContext())
                     .Throws(new InvalidOperationException("no DB in unit tests"));

        var server = new ServerState();

        if (type == "loading")
            await handler.HandleAsync(type, "dod_strand", server, 0, isReplay: true);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(type, "dod_strand", server, 0, isReplay: true));

        server.CurrentMap.Should().Be("dod_strand");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (MapChangeHandler handler, Mock<IDbContextFactory<HLStatsDbContext>> dbFactory) BuildHandler()
    {
        var dbFactory  = new Mock<IDbContextFactory<HLStatsDbContext>>();
        var clanSvc    = new ClanService(dbFactory.Object, NullLogger<ClanService>.Instance);
        var livestat   = new LivestatService(dbFactory.Object);
        var playerDb   = new PlayerDbService(
            dbFactory.Object, clanSvc, livestat, NullLogger<PlayerDbService>.Instance);
        var a2s        = new A2SQueryService();
        var udpListener = new UdpListener("0.0.0.0", 0, stdinMode: true, stdinServerAddress: "", inputFile: "",
            NullLogger<UdpListener>.Instance);
        var broadcast  = new ServerBroadcastService(
            NullLoggerFactory.Instance, NullLogger<ServerBroadcastService>.Instance, udpListener);
        var teamBonus  = new TeamBonusHandler(dbFactory.Object, broadcast);
        var playerAction = new PlayerActionHandler(dbFactory.Object, teamBonus, broadcast);
        var handler    = new MapChangeHandler(
            playerDb, playerAction, dbFactory.Object, a2s, NullLogger<MapChangeHandler>.Instance);
        return (handler, dbFactory);
    }
}
