using FluentAssertions;
using HLStatsX.NET.Daemon.Parsing;

namespace HLStatsX.NET.Tests.Daemon.Parsing;

public sealed class SteamIdNormalizerTests
{
    [Theory]
    [InlineData("STEAM_0:1:12345", "1:12345")]
    [InlineData("STEAM_1:0:67890", "0:67890")]
    [InlineData("STEAM_2:1:99999", "1:99999")]
    public void Normalize_SteamIdLegacyFormat_StripsUniversePrefix(string input, string expected)
    {
        SteamIdNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("[U:1:24691]", "1:12345")]   // N=24691, Y=24691%2=1, Z=24691/2=12345
    [InlineData("[U:1:135780]", "0:67890")]  // N=135780, Y=0, Z=67890
    [InlineData("[U:1:199999]", "1:99999")]  // N=199999, Y=1, Z=99999
    public void Normalize_SteamId3Format_ConvertsToSteam2Style(string input, string expected)
    {
        SteamIdNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("BOT")]
    [InlineData("UNKNOWN")]
    [InlineData("STEAM_ID_PENDING")]
    [InlineData("")]
    public void Normalize_NonSteamId_ReturnsUnchanged(string input)
    {
        SteamIdNormalizer.Normalize(input).Should().Be(input);
    }

    [Fact]
    public void Normalize_AlreadyNormalized_ReturnsUnchanged()
    {
        // Already stripped to Y:Z form — should return as-is
        SteamIdNormalizer.Normalize("1:12345").Should().Be("1:12345");
    }
}
