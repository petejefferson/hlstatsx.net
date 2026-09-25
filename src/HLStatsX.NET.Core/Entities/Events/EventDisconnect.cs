namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>Records a player disconnecting from a server. Paired with <see cref="EventConnect"/> to calculate session duration.</summary>
public class EventDisconnect
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
