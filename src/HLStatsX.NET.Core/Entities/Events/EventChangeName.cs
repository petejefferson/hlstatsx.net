namespace HLStatsX.NET.Core.Entities.Events;

public class EventChangeName
{
    public long Id { get; set; }
    public DateTime? EventTime { get; set; }
    public int ServerId { get; set; }
    public string Map { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
}
