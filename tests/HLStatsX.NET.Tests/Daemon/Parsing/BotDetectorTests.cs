using FluentAssertions;
using HLStatsX.NET.Daemon.Parsing;

namespace HLStatsX.NET.Tests.Daemon.Parsing;

public sealed class BotDetectorTests
{
    [Theory]
    [InlineData("BOT")]
    [InlineData("0")]
    [InlineData("00000000:0:0")]
    [InlineData("00000000:1:0")]
    [InlineData("00000000:99:0")]
    public void IsBot_KnownBotIds_ReturnsTrue(string uniqueId)
    {
        BotDetector.IsBot(uniqueId).Should().BeTrue();
    }

    [Theory]
    [InlineData("STEAM_0:1:12345")]
    [InlineData("1:12345")]
    [InlineData("UNKNOWN")]
    [InlineData("STEAM_ID_PENDING")]
    [InlineData("STEAM_ID_LAN")]
    [InlineData("")]
    public void IsBot_NonBotIds_ReturnsFalse(string uniqueId)
    {
        BotDetector.IsBot(uniqueId).Should().BeFalse();
    }

    [Theory]
    [InlineData("00000001:0:0")]   // wrong first segment
    [InlineData("00000000:0:1")]   // last segment not 0
    [InlineData("0:0:0")]          // too short
    public void IsBot_NearMissBotPatterns_ReturnsFalse(string uniqueId)
    {
        BotDetector.IsBot(uniqueId).Should().BeFalse();
    }
}
