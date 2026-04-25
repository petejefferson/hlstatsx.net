namespace HLStatsX.NET.Core.Models;

public record AdminEvent(
    string EventType,
    DateTime EventTime,
    string Message,
    string ServerName,
    string? Map
);

public class ResetOptions
{
    public bool ClearAwards { get; set; }
    public bool ClearHistory { get; set; }
    public bool ClearPlayerNames { get; set; }
    public bool ClearSkill { get; set; }
    public bool ClearCounts { get; set; }
    public bool ClearMapData { get; set; }
    public bool ClearBans { get; set; }
    public bool ClearEvents { get; set; }
    public bool DeletePlayers { get; set; }
}
