namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a player switching to a different role or class within a round
/// (e.g. changing from Rifleman to Sniper in Day of Defeat: Source).
/// </summary>
public class EventChangeRole
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
}
