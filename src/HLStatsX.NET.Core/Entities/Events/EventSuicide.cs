namespace HLStatsX.NET.Core.Entities.Events;

public class EventSuicide
{
    public long EventId { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string WeaponCode { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string Game { get; set; } = string.Empty;
    public int SkillChange { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
