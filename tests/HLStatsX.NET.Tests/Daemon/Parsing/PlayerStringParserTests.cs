using FluentAssertions;
using HLStatsX.NET.Daemon.Parsing;

namespace HLStatsX.NET.Tests.Daemon.Parsing;

public sealed class PlayerStringParserTests
{
    private const string ServerAddr = "192.168.1.1:27015";

    // --- Full 4-segment format ---

    [Fact]
    public void Parse_FullFormat_ReturnsAllFields()
    {
        var info = PlayerStringParser.Parse("PlayerName<5><STEAM_0:1:12345><CT>", ServerAddr);

        info.Should().NotBeNull();
        info!.Name.Should().Be("PlayerName");
        info.UserId.Should().Be(5);
        info.UniqueId.Should().Be("1:12345");  // normalised
        info.Team.Should().Be("CT");
        info.Role.Should().BeEmpty();
        info.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Parse_FullFormatWithRole_ReturnsRole()
    {
        var info = PlayerStringParser.Parse("Sniper<3><STEAM_0:0:99><Axis><Rifleman>", ServerAddr);

        info.Should().NotBeNull();
        info!.Role.Should().Be("Rifleman");
        info.Team.Should().Be("Axis");
    }

    [Fact]
    public void Parse_NameWithAngleBrackets_ParsedCorrectly()
    {
        // Edge case: player name contains <> characters
        var info = PlayerStringParser.Parse("<Elite><7><STEAM_0:1:42><T>", ServerAddr);

        info.Should().NotBeNull();
        info!.UserId.Should().Be(7);
        info.Team.Should().Be("T");
    }

    [Fact]
    public void Parse_ConsoleLine_ReturnsNull()
    {
        // Console log lines have uniqueid="Console" and team="Console"
        var info = PlayerStringParser.Parse("Console<0><Console><Console>", ServerAddr);

        info.Should().BeNull();
    }

    // --- Bot handling ---

    [Fact]
    public void Parse_BotUniqueId_IsDetectedAsBot()
    {
        var info = PlayerStringParser.Parse("BotPlayer<8><BOT><CT>", ServerAddr);

        info.Should().NotBeNull();
        info!.IsBot.Should().BeTrue();
    }

    [Fact]
    public void Parse_BotUniqueId_GetsStableHashId()
    {
        var info = PlayerStringParser.Parse("BotPlayer<8><BOT><CT>", ServerAddr);

        info!.UniqueId.Should().StartWith("BOT:");
        info.UniqueId.Should().HaveLength(36); // "BOT:" + 32 hex chars
    }

    [Fact]
    public void Parse_SameBotNameSameServer_ProducesSameUniqueId()
    {
        var info1 = PlayerStringParser.Parse("Terminator<1><BOT><T>", ServerAddr);
        var info2 = PlayerStringParser.Parse("Terminator<2><BOT><CT>", ServerAddr);

        // Same name + server → same hash, regardless of userid
        info1!.UniqueId.Should().Be(info2!.UniqueId);
    }

    [Fact]
    public void Parse_DifferentBotNamesSameServer_ProduceDifferentUniqueIds()
    {
        var info1 = PlayerStringParser.Parse("Alpha<1><BOT><T>", ServerAddr);
        var info2 = PlayerStringParser.Parse("Beta<1><BOT><T>", ServerAddr);

        info1!.UniqueId.Should().NotBe(info2!.UniqueId);
    }

    // --- SteamID normalisation ---

    [Fact]
    public void Parse_SteamId3Format_NormalisesToSteam2Style()
    {
        var info = PlayerStringParser.Parse("Player<1><[U:1:24691]><CT>", ServerAddr);

        info!.UniqueId.Should().Be("1:12345");
        info.PlainUniqueId.Should().Be("[U:1:24691]");  // raw stored separately
    }

    // --- Pending IDs ---

    [Theory]
    [InlineData("STEAM_ID_PENDING")]
    [InlineData("STEAM_ID_LAN")]
    [InlineData("VALVE_ID_PENDING")]
    [InlineData("VALVE_ID_LAN")]
    [InlineData("UNKNOWN")]
    public void IsPending_KnownPendingIds_ReturnsTrue(string uniqueId)
    {
        PlayerStringParser.IsPending(uniqueId).Should().BeTrue();
    }

    [Theory]
    [InlineData("1:12345")]
    [InlineData("BOT")]
    [InlineData("STEAM_0:1:12345")]
    public void IsPending_NonPendingIds_ReturnsFalse(string uniqueId)
    {
        PlayerStringParser.IsPending(uniqueId).Should().BeFalse();
    }

    // --- Short format ---

    [Fact]
    public void Parse_ShortFormat_ReturnsNameAndUniqueId()
    {
        var info = PlayerStringParser.Parse("Player<1:12345>", ServerAddr);

        info.Should().NotBeNull();
        info!.Name.Should().Be("Player");
        info.UniqueId.Should().Be("1:12345");
    }

    // --- Edge cases ---

    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {
        PlayerStringParser.Parse("", ServerAddr).Should().BeNull();
    }

    [Fact]
    public void Parse_NullString_ReturnsNull()
    {
        PlayerStringParser.Parse(null!, ServerAddr).Should().BeNull();
    }
}
