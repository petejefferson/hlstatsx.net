namespace HLStatsX.NET.Web.Helpers;

public static class RazorHelpers
{
    public static string FormatTime(int seconds) => FormatTime((long)seconds);

    public static string FormatTime(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        var parts = new List<string>();
        if (ts.Days    > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours   > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        return parts.Count > 0 ? string.Join(" ", parts) : "0m";
    }

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
