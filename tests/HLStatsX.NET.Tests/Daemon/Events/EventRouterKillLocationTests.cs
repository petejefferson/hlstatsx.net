using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.Geo;
using HLStatsX.NET.Daemon.Network;
using HLStatsX.NET.Daemon.Query;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Daemon.Events;

/// <summary>
/// Tests for the "World triggered killlocation" carry-forward mechanism in
/// <see cref="EventRouter"/>. DoD:S emits position data in this separate line
/// before the kill line; the daemon stores it in <see cref="ServerState"/> and
/// consumes it on the next kill event.
/// </summary>
public sealed class EventRouterKillLocationTests
{
    private static readonly DaemonOptions DefaultOptions = new();

    // ── killlocation stores attacker + victim positions on server state ───────

    [Fact]
    public async Task Dispatch_KillLocation_SetsAttackerPosition()
    {
        var (router, server) = BuildRouterWithServer();

        await router.DispatchAsync(
            """World triggered "killlocation" (attacker_position "123 456 789")(victim_position "0 0 0")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        server.NextKillX.Should().Be(123);
        server.NextKillY.Should().Be(456);
        server.NextKillZ.Should().Be(789);
    }

    [Fact]
    public async Task Dispatch_KillLocation_SetsVictimPosition()
    {
        var (router, server) = BuildRouterWithServer();

        await router.DispatchAsync(
            """World triggered "killlocation" (attacker_position "0 0 0")(victim_position "-100 200 -300")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        server.NextKillVicX.Should().Be(-100);
        server.NextKillVicY.Should().Be(200);
        server.NextKillVicZ.Should().Be(-300);
    }

    [Fact]
    public async Task Dispatch_KillLocation_SetsBothPositions()
    {
        var (router, server) = BuildRouterWithServer();

        await router.DispatchAsync(
            """World triggered "killlocation" (attacker_position "10 20 30")(victim_position "40 50 60")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        server.NextKillX.Should().Be(10);
        server.NextKillY.Should().Be(20);
        server.NextKillZ.Should().Be(30);
        server.NextKillVicX.Should().Be(40);
        server.NextKillVicY.Should().Be(50);
        server.NextKillVicZ.Should().Be(60);
    }

    [Fact]
    public async Task Dispatch_KillLocation_NegativeCoordinates_ParsedCorrectly()
    {
        var (router, server) = BuildRouterWithServer();

        await router.DispatchAsync(
            """World triggered "killlocation" (attacker_position "-1234 -5678 -9012")(victim_position "9999 -1 0")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        server.NextKillX.Should().Be(-1234);
        server.NextKillY.Should().Be(-5678);
        server.NextKillZ.Should().Be(-9012);
        server.NextKillVicX.Should().Be(9999);
        server.NextKillVicY.Should().Be(-1);
        server.NextKillVicZ.Should().Be(0);
    }

    [Fact]
    public async Task Dispatch_KillLocation_MissingAttackerPosition_DoesNotSetAttackerFields()
    {
        var (router, server) = BuildRouterWithServer();
        server.NextKillX = 99;

        await router.DispatchAsync(
            """World triggered "killlocation" (victim_position "1 2 3")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        // attacker fields unchanged
        server.NextKillX.Should().Be(99);
        // victim fields set
        server.NextKillVicX.Should().Be(1);
    }

    [Fact]
    public async Task Dispatch_KillLocation_DoesNotAffectOtherWorldTriggeredEvents()
    {
        var (router, server) = BuildRouterWithServer();

        // "Round_Win" should not set any NextKill coords
        await router.DispatchAsync(
            """World triggered "Round_Win" (winner "Allies")""",
            MakeCtx(server), DefaultOptions, serverAddr: "127.0.0.1:27015");

        server.NextKillX.Should().BeNull();
        server.NextKillVicX.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EventContext MakeCtx(ServerState server) => new()
    {
        Server    = server,
        EventUnix = 0,
        Map       = "dod_anzio",
        IsReplay  = true,
    };

    private static (EventRouter router, ServerState server) BuildRouterWithServer()
    {
        var dbFactory = new Mock<IDbContextFactory<HLStatsDbContext>>();

        var logFact    = NullLoggerFactory.Instance;
        var udpListener = new UdpListener(
            "0.0.0.0", 0, stdinMode: true, stdinServerAddress: "", inputFile: "",
            NullLogger<UdpListener>.Instance);
        var broadcast  = new ServerBroadcastService(
            logFact, NullLogger<ServerBroadcastService>.Instance, udpListener);

        var clanSvc   = new ClanService(dbFactory.Object, NullLogger<ClanService>.Instance);
        var liveStat  = new LivestatService(dbFactory.Object);
        var playerDb  = new PlayerDbService(
            dbFactory.Object, clanSvc, liveStat, NullLogger<PlayerDbService>.Instance);
        var geoIp     = new GeoIpLookup(dbFactory.Object, NullLogger<GeoIpLookup>.Instance);
        var a2s       = new A2SQueryService();
        var teamBonus = new TeamBonusHandler(dbFactory.Object, broadcast);

        var router = new EventRouter(
            new ConnectHandler(playerDb, geoIp, liveStat, broadcast, dbFactory.Object,
                NullLogger<ConnectHandler>.Instance),
            new EnterGameHandler(dbFactory.Object, broadcast, NullLogger<EnterGameHandler>.Instance),
            new DisconnectHandler(playerDb, new PlayerActionHandler(dbFactory.Object, teamBonus, broadcast),
                liveStat, dbFactory.Object, NullLogger<DisconnectHandler>.Instance),
            new ChangeTeamHandler(dbFactory.Object),
            new ChangeRoleHandler(dbFactory.Object),
            new ChangeNameHandler(playerDb, clanSvc, dbFactory.Object),
            new FragHandler(dbFactory.Object, new PlayerActionHandler(dbFactory.Object, teamBonus, broadcast),
                broadcast, NullLogger<FragHandler>.Instance),
            new SuicideHandler(dbFactory.Object),
            new ChatHandler(dbFactory.Object),
            new PlayerActionHandler(dbFactory.Object, teamBonus, broadcast),
            new MapChangeHandler(playerDb, new PlayerActionHandler(dbFactory.Object, teamBonus, broadcast),
                dbFactory.Object, a2s, NullLogger<MapChangeHandler>.Instance),
            new StatsmeHandler(dbFactory.Object),
            teamBonus,
            dbFactory.Object,
            NullLogger<EventRouter>.Instance);

        var server = new ServerState
        {
            ServerId = 1,
            Address  = "127.0.0.1",
            Port     = 27015,
            Game     = "dods",
        };

        return (router, server);
    }
}
