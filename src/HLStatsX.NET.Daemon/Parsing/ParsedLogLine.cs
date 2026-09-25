namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// A successfully decoded log line with its timestamp stripped.
/// <c>EventText</c> is the content after <c>"L MM/DD/YYYY - HH:MM:SS: "</c>.
/// </summary>
public readonly struct ParsedLogLine
{
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Event text with the leading timestamp removed.</summary>
    public string EventText { get; init; }
}
