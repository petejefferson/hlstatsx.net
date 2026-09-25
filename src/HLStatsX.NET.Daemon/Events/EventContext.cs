using HLStatsX.NET.Daemon.State;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Carries the per-event shared context passed to every event handler.
/// </summary>
/// <remarks>
/// Mirrors the Perl daemon's set of globals that are updated at the top of the main
/// processing loop before any handler is called:
/// <c>$s_addr</c>, <c>$ev_unixtime</c>, <c>$ev_remotetime</c>, and <c>$g_servers{$s_addr}</c>.
/// </remarks>
public sealed class EventContext
{
    /// <summary>The game server that sent this log event.</summary>
    public required ServerState Server { get; init; }

    /// <summary>
    /// Wall-clock unix timestamp of the event. When <c>UseTimestamp</c> is true
    /// this comes from the log header; otherwise it is <c>DateTimeOffset.UtcNow</c>.
    /// </summary>
    public long EventUnix { get; init; }

    /// <summary>The map the server was running when this event was emitted.</summary>
    public string Map { get; init; } = string.Empty;

    /// <summary>
    /// True when the daemon is replaying from a file or stdin rather than live UDP.
    /// Handlers use this to skip operations that require a live server (A2S, RCON).
    /// </summary>
    public bool IsReplay { get; init; }

    /// <summary>
    /// Local <see cref="DateTime"/> derived from <see cref="EventUnix"/>, for use
    /// when populating EF Core entity <c>EventTime</c> columns.
    /// Perl uses <c>localtime()</c> so we match that here.
    /// </summary>
    public DateTime EventTime =>
        DateTimeOffset.FromUnixTimeSeconds(EventUnix).LocalDateTime;
}
