namespace HLStatsX.NET.Core.Entities.Events;

public class EventEntry
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string? Map { get; set; }
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
