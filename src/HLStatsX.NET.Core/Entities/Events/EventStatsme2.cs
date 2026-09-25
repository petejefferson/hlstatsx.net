namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Stores per-weapon hit-location data reported by the StatsMe2 plugin.
/// Each property is a hit count for a specific body part and maps to a column in
/// <c>hlstats_Events_Statsme2</c>. Used for the hit-location breakdown on player profiles.
/// </summary>
public class EventStatsme2
{
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Weapon { get; set; } = string.Empty;
    public int Head { get; set; }
    public int Chest { get; set; }
    public int Stomach { get; set; }
    public int LeftArm { get; set; }
    public int RightArm { get; set; }
    public int LeftLeg { get; set; }
    public int RightLeg { get; set; }
}
