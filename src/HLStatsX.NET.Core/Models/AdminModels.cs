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

/// <summary>
/// Represents a single row in the IP stats host-group list view.
/// Each row groups all connection events that share the same <paramref name="HostGroup"/>
/// (ISP / organisation name as resolved by the daemon).
/// </summary>
/// <param name="HostGroup">
/// Human-readable ISP or organisation name. Empty strings are surfaced as
/// <c>"(Unresolved IP Addresses)"</c> by the repository before returning.
/// </param>
/// <param name="Connects">Total connection events recorded for this host group.</param>
/// <param name="Percent">Percentage of all-time connections this group represents (0–100).</param>
public record IpStatsHostGroupRow(string HostGroup, int Connects, double Percent);

/// <summary>
/// Represents a single row in the IP stats host drill-down view.
/// Each row is one distinct hostname or IP address within a specific host group.
/// </summary>
/// <param name="Host">
/// Resolved hostname when available; falls back to the raw IP address when the
/// hostname field is empty (mirrors <c>IF(hostname='', ipAddress, hostname)</c> in PHP).
/// </param>
/// <param name="Connects">Total connection events recorded for this host.</param>
/// <param name="Percent">Percentage of connections within the parent host group (0–100).</param>
public record IpStatsHostRow(string Host, int Connects, double Percent);
