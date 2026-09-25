using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Tests.Daemon.Rcon;

public sealed class ServerBroadcastServiceTests
{
    private static (ServerBroadcastService svc, Mock<IRconClient> client) BuildSvc()
    {
        var mockClient = new Mock<IRconClient>();
        mockClient
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var logger = new Mock<ILogger<ServerBroadcastService>>();
        var svc = new ServerBroadcastService(loggerFactory.Object, logger.Object, _ => mockClient.Object);
        return (svc, mockClient);
    }

    private static ServerState BuildServer(ServerConfig config) => new()
    {
        ServerId = 1,
        Address = "192.168.1.1",
        Port = 27015,
        Game = "dods",
        Name = "Test Server",
        CurrentMap = "dod_flash",
        Config = config,
    };

    private static PlayerSession BuildPlayer(
        bool isBot = false, int userId = 5,
        bool displayEvents = true, bool broadcastEvents = true) => new()
    {
        UserId = userId,
        UniqueId = "STEAM_0:1:12345",
        Name = "TestPlayer",
        IsBot = isBot,
        DisplayEvents = displayEvents,
    };

    // --- MessageAllAsync guards ---

    [Fact]
    public async Task MessageAllAsync_SendsCommand_WhenBroadcastEventsEnabled()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });

        await svc.MessageAllAsync(server, "hello");

        client.Verify(c => c.ExecuteAsync(It.Is<string>(s => s.Length > 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MessageAllAsync_SkipsCommand_WhenBroadcastEventsDisabled()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = false });

        await svc.MessageAllAsync(server, "hello", force: false);

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MessageAllAsync_SendsCommand_WhenForced_EvenIfBroadcastDisabled()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = false });

        await svc.MessageAllAsync(server, "hello", force: true);

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MessageAllAsync_UsesAnnounceCommand_FromConfig()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig
        {
            BroadcastEvents = true,
            BroadcastEventsCommandAnnounce = "sm_csay",
        });

        await svc.MessageAllAsync(server, "hello");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => s.StartsWith("sm_csay")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MessageAllAsync_StripsSemicolons_FromMessage()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });

        await svc.MessageAllAsync(server, "hello;world");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => !s.Contains(';')), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- MessagePlayerAsync guards ---

    [Fact]
    public async Task MessagePlayerAsync_SendsCommand_WhenAllConditionsMet()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });
        var player = BuildPlayer(isBot: false, userId: 5, displayEvents: true);

        await svc.MessagePlayerAsync(server, player, "you did it");

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MessagePlayerAsync_Skips_WhenBroadcastDisabled()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = false });
        var player = BuildPlayer(userId: 5, displayEvents: true);

        await svc.MessagePlayerAsync(server, player, "msg");

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MessagePlayerAsync_Skips_WhenDisplayEventsDisabled()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });
        var player = BuildPlayer(userId: 5, displayEvents: false);

        await svc.MessagePlayerAsync(server, player, "msg");

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MessagePlayerAsync_Skips_WhenPlayerIsBot()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });
        var player = BuildPlayer(isBot: true, userId: 5, displayEvents: true);

        await svc.MessagePlayerAsync(server, player, "msg");

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MessagePlayerAsync_Skips_WhenUserIdIsZero()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });
        var player = BuildPlayer(userId: 0, displayEvents: true);

        await svc.MessagePlayerAsync(server, player, "msg");

        client.Verify(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MessagePlayerAsync_IncludesUserId_InCommand()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { BroadcastEvents = true });
        var player = BuildPlayer(userId: 42, displayEvents: true);

        await svc.MessagePlayerAsync(server, player, "nice shot");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => s.Contains("42")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- KickPlayerAsync ---

    [Fact]
    public async Task KickPlayerAsync_UsesKickId_ForSourceEngine()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { GameEngine = 2 });
        var player = BuildPlayer(userId: 7);

        await svc.KickPlayerAsync(server, player, "Reason");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => s == "kickid 7 Reason"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KickPlayerAsync_UsesKickHash_ForGoldSrc()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { GameEngine = 1 });
        var player = BuildPlayer(userId: 7);

        await svc.KickPlayerAsync(server, player, "Reason");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => s == "kick #7"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KickPlayerAsync_StripsSemicolons_FromReason()
    {
        var (svc, client) = BuildSvc();
        var server = BuildServer(new ServerConfig { GameEngine = 2 });
        var player = BuildPlayer(userId: 3);

        await svc.KickPlayerAsync(server, player, "bad;player");

        client.Verify(
            c => c.ExecuteAsync(It.Is<string>(s => !s.Contains(';')), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- Client management ---

    [Fact]
    public async Task DisposeServerClientAsync_RemovesClient_FromPool()
    {
        int factoryCallCount = 0;
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        var logger = new Mock<ILogger<ServerBroadcastService>>();

        var svc = new ServerBroadcastService(loggerFactory.Object, logger.Object, _ =>
        {
            factoryCallCount++;
            var mockClient = new Mock<IRconClient>();
            mockClient
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
            mockClient
                .Setup(c => c.DisposeAsync())
                .Returns(ValueTask.CompletedTask);
            return mockClient.Object;
        });

        var server = BuildServer(new ServerConfig { BroadcastEvents = true });

        await svc.ExecuteAsync(server, "status");
        factoryCallCount.Should().Be(1);

        await svc.DisposeServerClientAsync("192.168.1.1:27015");

        await svc.ExecuteAsync(server, "status");
        factoryCallCount.Should().Be(2, "a second client should be created after disposing the first");
    }
}
