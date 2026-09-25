namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a ping sample for a player at a point in time.
/// Ping samples are collected periodically by the stats daemon and used to compute
/// the average ping shown on the player profile.
/// </summary>
public class EventLatency
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public int Ping { get; set; }
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
}
