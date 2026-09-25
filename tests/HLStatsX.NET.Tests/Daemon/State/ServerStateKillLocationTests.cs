using HLStatsX.NET.Daemon.State;

namespace HLStatsX.NET.Tests.Daemon.State;

/// <summary>
/// Tests for the kill-location carry-forward fields on <see cref="ServerState"/>.
/// These fields mirror Perl's nextkillx/nextkillvicx server globals, which are populated
/// by "World triggered killlocation" events and consumed on the next kill.
/// </summary>
public sealed class ServerStateKillLocationTests
{
    [Fact]
    public void NextKillFields_InitiallyAllNull()
    {
        var server = new ServerState();

        server.NextKillX.Should().BeNull();
        server.NextKillY.Should().BeNull();
        server.NextKillZ.Should().BeNull();
        server.NextKillVicX.Should().BeNull();
        server.NextKillVicY.Should().BeNull();
        server.NextKillVicZ.Should().BeNull();
    }

    [Fact]
    public void NextKillFields_CanBeSet()
    {
        var server = new ServerState();

        server.NextKillX    = 123;
        server.NextKillY    = -456;
        server.NextKillZ    = 789;
        server.NextKillVicX = -100;
        server.NextKillVicY = 200;
        server.NextKillVicZ = -300;

        server.NextKillX.Should().Be(123);
        server.NextKillY.Should().Be(-456);
        server.NextKillZ.Should().Be(789);
        server.NextKillVicX.Should().Be(-100);
        server.NextKillVicY.Should().Be(200);
        server.NextKillVicZ.Should().Be(-300);
    }

    [Fact]
    public void NextKillFields_CanBeResetToNull()
    {
        var server = new ServerState
        {
            NextKillX    = 1,
            NextKillY    = 2,
            NextKillZ    = 3,
            NextKillVicX = 4,
            NextKillVicY = 5,
            NextKillVicZ = 6,
        };

        // Simulate the post-kill reset that EventRouter performs
        server.NextKillX = server.NextKillY = server.NextKillZ = null;
        server.NextKillVicX = server.NextKillVicY = server.NextKillVicZ = null;

        server.NextKillX.Should().BeNull();
        server.NextKillY.Should().BeNull();
        server.NextKillZ.Should().BeNull();
        server.NextKillVicX.Should().BeNull();
        server.NextKillVicY.Should().BeNull();
        server.NextKillVicZ.Should().BeNull();
    }

    [Fact]
    public void NextKillFields_AcceptNegativeCoordinates()
    {
        var server = new ServerState();

        server.NextKillX = int.MinValue;
        server.NextKillZ = int.MaxValue;

        server.NextKillX.Should().Be(int.MinValue);
        server.NextKillZ.Should().Be(int.MaxValue);
    }
}
