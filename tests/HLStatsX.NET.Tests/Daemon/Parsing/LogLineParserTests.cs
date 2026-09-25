using FluentAssertions;
using HLStatsX.NET.Daemon.Parsing;

namespace HLStatsX.NET.Tests.Daemon.Parsing;

public sealed class LogLineParserTests
{
    [Fact]
    public void TryParse_StandardFormat_ReturnsTimestampAndEventText()
    {
        const string raw = "L 01/15/2024 - 14:30:45: \"Player<1><STEAM_0:1:12345><CT>\" killed \"Victim<2><STEAM_0:0:67890><T>\" with \"ak47\"";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        // Timestamp is parsed as local time (matching Perl's timelocal()), so the offset
        // equals the system's local UTC offset for that instant — not necessarily zero.
        var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 15, 14, 30, 45));
        result.Timestamp.Should().Be(new DateTimeOffset(2024, 1, 15, 14, 30, 45, expectedOffset));
        result.EventText.Should().Be("\"Player<1><STEAM_0:1:12345><CT>\" killed \"Victim<2><STEAM_0:0:67890><T>\" with \"ak47\"");
    }

    [Fact]
    public void TryParse_WithSourceEngineHeader_StripsLeadingBytes()
    {
        // Source engine prefixes with 0xFF 0xFF 0xFF 0xFF — represented here as arbitrary chars
        const string raw = "\xff\xff\xff\xffL 05/09/2026 - 10:00:00: World triggered \"RoundStart\"";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.Timestamp.Year.Should().Be(2026);
        result.EventText.Should().Be("World triggered \"RoundStart\"");
    }

    [Fact]
    public void TryParse_WithCarriageReturnAndNewline_StripsNoise()
    {
        const string raw = "L 03/20/2025 - 08:15:00: Player connected\r\n";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().Be("Player connected");
    }

    [Fact]
    public void TryParse_WithNullBytes_StripsNoise()
    {
        const string raw = "L 03/20/2025 - 08:15:00: Player connected\0";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().NotContain("\0");
    }

    [Fact]
    public void TryParse_WithNoCDTag_StripsTag()
    {
        const string raw = "[No.C-D]L 01/01/2024 - 00:00:01: World triggered \"RoundEnd\"";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().Be("World triggered \"RoundEnd\"");
    }

    [Fact]
    public void TryParse_WithOldCDTag_StripsTag()
    {
        const string raw = "[OLD.C-D]L 01/01/2024 - 00:00:01: World triggered \"RoundEnd\"";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().Be("World triggered \"RoundEnd\"");
    }

    [Fact]
    public void TryParse_WithNoCLTag_StripsTag()
    {
        const string raw = "[NOCL]L 01/01/2024 - 00:00:01: World triggered \"RoundEnd\"";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().Be("World triggered \"RoundEnd\"");
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        LogLineParser.TryParse("", out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_NoTimestamp_ReturnsFalse()
    {
        LogLineParser.TryParse("This is not a log line", out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_EmptyEventText_ReturnsEmptyString()
    {
        const string raw = "L 06/01/2024 - 12:00:00: ";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.EventText.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_TimestampFields_ParsedCorrectly()
    {
        const string raw = "L 12/31/2023 - 23:59:59: event";

        LogLineParser.TryParse(raw, out var result).Should().BeTrue();
        result.Timestamp.Month.Should().Be(12);
        result.Timestamp.Day.Should().Be(31);
        result.Timestamp.Year.Should().Be(2023);
        result.Timestamp.Hour.Should().Be(23);
        result.Timestamp.Minute.Should().Be(59);
        result.Timestamp.Second.Should().Be(59);
    }
}
