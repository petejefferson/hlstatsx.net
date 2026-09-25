using HLStatsX.NET.Daemon.State;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Daemon.State;

public class DaemonStateManagerTests
{
    // ── TryParseSenderAddr ────────────────────────────────────────────────────

    [Theory]
    [InlineData("192.168.1.1:27015", "192.168.1.1", 27015)]
    [InlineData("10.0.0.1:27500",    "10.0.0.1",    27500)]
    [InlineData("255.255.255.255:1", "255.255.255.255", 1)]
    public void TryParseSenderAddr_ValidIpv4_ReturnsTrueAndComponents(
        string input, string expectedAddress, int expectedPort)
    {
        var ok = DaemonStateManager.TryParseSenderAddr(input, out var address, out var port);

        ok.Should().BeTrue();
        address.Should().Be(expectedAddress);
        port.Should().Be(expectedPort);
    }

    [Fact]
    public void TryParseSenderAddr_IPv6BracketNotation_ReturnsTrueAndComponents()
    {
        var ok = DaemonStateManager.TryParseSenderAddr("[::1]:27015", out var address, out var port);

        ok.Should().BeTrue();
        address.Should().Be("[::1]");
        port.Should().Be(27015);
    }

    [Theory]
    [InlineData("")]
    [InlineData("noport")]
    [InlineData(":")]
    [InlineData(":abc")]
    public void TryParseSenderAddr_InvalidInput_ReturnsFalse(string input)
    {
        DaemonStateManager.TryParseSenderAddr(input, out _, out _).Should().BeFalse();
    }

    // ── AutoRegisterGame property ─────────────────────────────────────────────

    [Fact]
    public void AutoRegisterGame_NotConfigured_ReturnsEmpty()
    {
        var mgr = BuildManager(game: null);
        mgr.AutoRegisterGame.Should().BeEmpty();
    }

    [Fact]
    public void AutoRegisterGame_Configured_ReturnsValue()
    {
        var mgr = BuildManager(game: "dods");
        mgr.AutoRegisterGame.Should().Be("dods");
    }

    // ── AutoRegisterServerAsync — early exits ─────────────────────────────────

    [Fact]
    public async Task AutoRegisterServerAsync_NoGameConfigured_ReturnsNull()
    {
        var mgr = BuildManager(game: null);

        var result = await mgr.AutoRegisterServerAsync("1.2.3.4:27015");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AutoRegisterServerAsync_UnparsableAddr_ReturnsNull()
    {
        var mgr = BuildManager(game: "dods");

        var result = await mgr.AutoRegisterServerAsync("not-an-address");

        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DaemonStateManager BuildManager(string? game)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(game is null
                ? []
                : new Dictionary<string, string?> { ["Daemon:AutoRegisterGame"] = game })
            .Build();

        var dbFactory = new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<HLStatsX.NET.Infrastructure.Data.HLStatsDbContext>>();

        return new DaemonStateManager(
            dbFactory.Object,
            config,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DaemonStateManager>.Instance);
    }
}
