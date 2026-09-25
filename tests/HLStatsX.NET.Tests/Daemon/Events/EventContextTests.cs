using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.State;

namespace HLStatsX.NET.Tests.Daemon.Events;

public sealed class EventContextTests
{
    // ── EventTime uses local time (matches Perl's localtime()) ───────────────

    [Fact]
    public void EventTime_ReturnsLocalDateTime()
    {
        long unix = 1_700_000_000L;
        var ctx = new EventContext { Server = new ServerState(), EventUnix = unix };

        var expected = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;

        ctx.EventTime.Should().Be(expected);
    }

    [Fact]
    public void EventTime_KindIsLocal()
    {
        var ctx = new EventContext { Server = new ServerState(), EventUnix = 1_000_000L };

        ctx.EventTime.Kind.Should().Be(DateTimeKind.Local);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1_000_000_000L)]
    [InlineData(1_700_000_000L)]
    public void EventTime_MatchesDateTimeOffsetLocalDateTime_ForVariousTimestamps(long unix)
    {
        var ctx = new EventContext { Server = new ServerState(), EventUnix = unix };

        ctx.EventTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime);
    }

    // ── EventTime is NOT UTC (regression: was previously UtcDateTime) ────────

    [Fact]
    public void EventTime_DoesNotEqualUtcTime_WhenOffsetNonZero()
    {
        long unix = 1_700_000_000L;
        var ctx = new EventContext { Server = new ServerState(), EventUnix = unix };

        var utc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        var local = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;

        // Only assert the difference when the local offset is non-zero;
        // on UTC hosts local == UTC, and this particular assertion is vacuous.
        if (TimeZoneInfo.Local.GetUtcOffset(local) != TimeSpan.Zero)
            ctx.EventTime.Should().NotBe(utc);
        else
            ctx.EventTime.Should().Be(utc); // UTC host: they're equal, that's fine
    }
}
