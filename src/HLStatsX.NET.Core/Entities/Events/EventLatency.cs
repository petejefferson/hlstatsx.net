namespace HLStatsX.NET.Core.Entities.Events;

public class EventLatency
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public int Ping { get; set; }
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
}
