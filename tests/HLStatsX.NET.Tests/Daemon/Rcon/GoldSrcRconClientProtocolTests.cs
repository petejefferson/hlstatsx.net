using HLStatsX.NET.Daemon.Rcon;

namespace HLStatsX.NET.Tests.Daemon.Rcon;

public sealed class GoldSrcRconClientProtocolTests
{
    [Fact]
    public void ParseChallengeNumber_ReturnsNumber_WhenValidResponse()
    {
        var result = GoldSrcRconClient.ParseChallengeNumber("challenge rcon 123456");

        result.Should().Be(123456);
    }

    [Fact]
    public void ParseChallengeNumber_ReturnsNull_WhenResponseHasNoMatch()
    {
        var result = GoldSrcRconClient.ParseChallengeNumber("bad response");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseChallengeNumber_HandlesLargeNumbers()
    {
        var result = GoldSrcRconClient.ParseChallengeNumber("challenge rcon 2147483647");

        result.Should().Be(2147483647);
    }
}
