namespace HLStatsX.NET.Core.Entities.Events;

public class EventFrag
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int KillerId { get; set; }
    public int VictimId { get; set; }
    public string Weapon { get; set; } = string.Empty;
    public bool Headshot { get; set; }
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string KillerRole { get; set; } = string.Empty;
    public string VictimRole { get; set; } = string.Empty;
    public int? WeaponId { get; set; }
    public int? MapId { get; set; }

    public Player? Killer { get; set; }
    public Player? Victim { get; set; }
    public Server? Server { get; set; }
}
