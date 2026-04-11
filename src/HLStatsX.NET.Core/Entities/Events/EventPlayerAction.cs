namespace HLStatsX.NET.Core.Entities.Events;

public class EventPlayerAction
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public int ActionId { get; set; }
    public int Bonus { get; set; }
}
