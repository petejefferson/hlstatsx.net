namespace HLStatsX.NET.Core.Entities.Events;

public class EventTeamBonus
{
    public long Id { get; set; }
    public DateTime? EventTime { get; set; }
    public int ServerId { get; set; }
    public string Map { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public int ActionId { get; set; }
    public int Bonus { get; set; }
}
