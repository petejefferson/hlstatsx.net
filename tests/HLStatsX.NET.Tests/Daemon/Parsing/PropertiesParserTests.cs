using FluentAssertions;
using HLStatsX.NET.Daemon.Parsing;

namespace HLStatsX.NET.Tests.Daemon.Parsing;

public sealed class PropertiesParserTests
{
    [Fact]
    public void Parse_QuotedStringValue_ParsedCorrectly()
    {
        var props = PropertiesParser.Parse("(weapon \"ak47\")");

        props.Should().ContainKey("weapon").WhoseValue.Should().Be("ak47");
    }

    [Fact]
    public void Parse_QuotedValueWithSpaces_ParsedCorrectly()
    {
        var props = PropertiesParser.Parse("(attacker_position \"-123 456 789\")");

        props["attacker_position"].Should().Be("-123 456 789");
    }

    [Fact]
    public void Parse_BooleanFlag_StoredAsOne()
    {
        var props = PropertiesParser.Parse("(headshot)");

        props.Should().ContainKey("headshot").WhoseValue.Should().Be("1");
    }

    [Fact]
    public void Parse_UnquotedValue_ParsedCorrectly()
    {
        var props = PropertiesParser.Parse("(damage 50)");

        props["damage"].Should().Be("50");
    }

    [Fact]
    public void Parse_MultipleProperties_AllParsed()
    {
        var props = PropertiesParser.Parse("(weapon \"ak47\") (headshot) (damage 85)");

        props.Should().HaveCount(3);
        props["weapon"].Should().Be("ak47");
        props["headshot"].Should().Be("1");
        props["damage"].Should().Be("85");
    }

    [Fact]
    public void Parse_NullInput_ReturnsEmpty()
    {
        PropertiesParser.Parse(null).Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceInput_ReturnsEmpty()
    {
        PropertiesParser.Parse("   ").Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        PropertiesParser.Parse("").Should().BeEmpty();
    }

    [Fact]
    public void Parse_KeyLookupIsCaseInsensitive()
    {
        var props = PropertiesParser.Parse("(Weapon \"ak47\")");

        props.Should().ContainKey("weapon");
        props.Should().ContainKey("WEAPON");
    }

    // DoDS flag capture: two "player" keys → player_a / player_b (matching Perl flagindex rename)
    [Fact]
    public void Parse_DuplicatePlayerKey_RenamedToPlayerAAndPlayerB()
    {
        var props = PropertiesParser.Parse("(player \"Alpha\") (player \"Beta\")");

        props.Should().ContainKey("player_a").WhoseValue.Should().Be("Alpha");
        props.Should().ContainKey("player_b").WhoseValue.Should().Be("Beta");
        props.Should().NotContainKey("player");
    }

    [Fact]
    public void Parse_TripleDuplicatePlayerKey_ThirdStoredAsPlayer()
    {
        // player_a = first, player_b = second, player = third (matches Perl: dods_flag > 2 → no rename)
        var props = PropertiesParser.Parse("(player \"Alpha\") (player \"Beta\") (player \"Gamma\")");

        props.Should().ContainKey("player_a").WhoseValue.Should().Be("Alpha");
        props.Should().ContainKey("player_b").WhoseValue.Should().Be("Beta");
        props.Should().ContainKey("player").WhoseValue.Should().Be("Gamma");
    }

    [Fact]
    public void Parse_RealWorldFragLine_ParsedCorrectly()
    {
        const string props = "(weapon \"ak47\") (headshot)";
        var result = PropertiesParser.Parse(props);

        result["weapon"].Should().Be("ak47");
        result["headshot"].Should().Be("1");
    }
}
