namespace HLStatsX.NET.Core.Entities.Events;

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
