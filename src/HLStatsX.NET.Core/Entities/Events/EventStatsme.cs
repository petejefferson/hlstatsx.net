namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Stores per-weapon shot and hit accuracy data reported by the StatsMe plugin
/// (commonly used with CS:S and DoD:S). This table is populated by the
/// <c>statsme</c> event and drives the weapon accuracy view on player profiles.
/// </summary>
public class EventStatsme
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Weapon { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public int Kills { get; set; }
    public int Hits { get; set; }
    public int Shots { get; set; }
    public int Headshots { get; set; }
    public int Deaths { get; set; }
    public int Damage { get; set; }
}
