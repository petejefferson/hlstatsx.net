namespace HLStatsX.NET.Core.Entities.Events;

public class EventChangeTeam
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Team { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
}
