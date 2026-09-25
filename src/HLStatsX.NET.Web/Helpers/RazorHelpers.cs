namespace HLStatsX.NET.Web.Helpers;

/// <summary>
/// Formatting helpers used in Razor views. Methods are static so they can be called
/// directly without injecting a service.
/// </summary>
public static class RazorHelpers
{
    /// <summary>
    /// Formats a duration in seconds as a compact string, e.g. <c>"2d 4h 30m"</c>.
    /// Returns <c>"0m"</c> when the duration is less than one minute.
    /// </summary>
    public static string FormatTime(int seconds) => FormatTime((long)seconds);

    /// <inheritdoc cref="FormatTime(int)"/>
    public static string FormatTime(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        var parts = new List<string>();
        if (ts.Days    > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours   > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        return parts.Count > 0 ? string.Join(" ", parts) : "0m";
    }

    /// <summary>
    /// Formats a duration in seconds as a long-form string, e.g.
    /// <c>"2 days, 4 hours, 30 minutes"</c> — suitable for tooltips or accessible text.
    /// Returns <c>"0 minutes"</c> when the duration is less than one minute.
    /// </summary>
    public static string FormatTimeFull(int seconds) => FormatTimeFull((long)seconds);

    public static string FormatTimeFull(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        var parts = new List<string>();
        if (ts.Days    > 0) parts.Add($"{ts.Days} day{(ts.Days != 1 ? "s" : "")}");
        if (ts.Hours   > 0) parts.Add($"{ts.Hours} hour{(ts.Hours != 1 ? "s" : "")}");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes} minute{(ts.Minutes != 1 ? "s" : "")}");
        return parts.Count > 0 ? string.Join(", ", parts) : "0 minutes";
    }
}
