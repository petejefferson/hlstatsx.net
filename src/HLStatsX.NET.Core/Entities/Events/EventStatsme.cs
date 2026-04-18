namespace HLStatsX.NET.Core.Entities.Events;

public class EventStatsme
{
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Weapon { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Hits { get; set; }
    public int Shots { get; set; }
    public int Headshots { get; set; }
    public int Deaths { get; set; }
    public int Damage { get; set; }
}
