using HLStatsX.NET.Web.Helpers;

namespace HLStatsX.NET.Tests.Helpers;

public class RazorHelpersTests
{
    // ── FormatTime ──────────────────────────────────────────────────────────

    [Fact]
    public void FormatTime_Returns0m_WhenZeroSeconds()
    {
        RazorHelpers.FormatTime(0).Should().Be("0m");
    }

    [Fact]
    public void FormatTime_ReturnsMinutesOnly_WhenUnderOneHour()
    {
        RazorHelpers.FormatTime(90).Should().Be("1m");  // 1 minute, 30s rounds down
    }

    [Fact]
    public void FormatTime_ReturnsHoursAndMinutes_WhenUnderOneDay()
    {
        RazorHelpers.FormatTime(3 * 3600 + 25 * 60).Should().Be("3h 25m");
    }

    [Fact]
    public void FormatTime_ReturnsHoursOnly_WhenExactHour()
    {
        RazorHelpers.FormatTime(2 * 3600).Should().Be("2h");
    }

    [Fact]
    public void FormatTime_ReturnsDaysHoursMinutes_WhenOverOneDay()
    {
        RazorHelpers.FormatTime(2 * 86400 + 3 * 3600 + 15 * 60).Should().Be("2d 3h 15m");
    }

    [Fact]
    public void FormatTime_ReturnsDaysOnly_WhenExactDays()
    {
        RazorHelpers.FormatTime(3 * 86400).Should().Be("3d");
    }

    [Fact]
    public void FormatTime_IntOverload_MatchesLongOverload()
    {
        RazorHelpers.FormatTime(3661).Should().Be(RazorHelpers.FormatTime((long)3661));
    }

    // ── FormatTimeFull ───────────────────────────────────────────────────────

    [Fact]
    public void FormatTimeFull_Returns0Minutes_WhenZeroSeconds()
    {
        RazorHelpers.FormatTimeFull(0).Should().Be("0 minutes");
    }

    [Fact]
    public void FormatTimeFull_UsesSingular_ForOneOfEach()
    {
        RazorHelpers.FormatTimeFull(86400 + 3600 + 60).Should().Be("1 day, 1 hour, 1 minute");
    }

    [Fact]
    public void FormatTimeFull_UsesPlural_ForMultipleOfEach()
    {
        RazorHelpers.FormatTimeFull(2 * 86400 + 3 * 3600 + 5 * 60).Should().Be("2 days, 3 hours, 5 minutes");
    }

    [Fact]
    public void FormatTimeFull_OmitsDays_WhenUnderOneDay()
    {
        RazorHelpers.FormatTimeFull(2 * 3600 + 30 * 60).Should().Be("2 hours, 30 minutes");
    }

    [Fact]
    public void FormatTimeFull_OmitsMinutes_WhenExactHours()
    {
        RazorHelpers.FormatTimeFull(4 * 3600).Should().Be("4 hours");
    }

    [Fact]
    public void FormatTimeFull_IntOverload_MatchesLongOverload()
    {
        RazorHelpers.FormatTimeFull(7384).Should().Be(RazorHelpers.FormatTimeFull((long)7384));
    }

    // ── FormatTime edge cases ────────────────────────────────────────────────

    [Fact]
    public void FormatTime_Returns0m_WhenLessThanOneMinute()
    {
        RazorHelpers.FormatTime(59).Should().Be("0m");
    }

    [Fact]
    public void FormatTime_Returns1m_WhenExactly60Seconds()
    {
        RazorHelpers.FormatTime(60).Should().Be("1m");
    }

    [Fact]
    public void FormatTime_Returns1h_WhenExactly3600Seconds()
    {
        RazorHelpers.FormatTime(3600).Should().Be("1h");
    }

    // ── FormatTimeFull edge cases ────────────────────────────────────────────

    [Fact]
    public void FormatTimeFull_Returns0Minutes_WhenUnderOneMinute()
    {
        RazorHelpers.FormatTimeFull(59).Should().Be("0 minutes");
    }

    [Fact]
    public void FormatTimeFull_Returns1Minute_WhenExactly60Seconds()
    {
        RazorHelpers.FormatTimeFull(60).Should().Be("1 minute");
    }
}
