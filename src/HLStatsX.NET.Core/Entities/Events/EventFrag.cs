namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// A kill (frag) event logged by the Perl daemon when one player kills another.
/// Maps to <c>hlstats_Events_Frags</c>. Stores the killer, victim, weapon used,
/// whether it was a headshot, and 3-D position data when the game provides it.
/// </summary>
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
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? PosZ { get; set; }
    public int? PosVictimX { get; set; }
    public int? PosVictimY { get; set; }
    public int? PosVictimZ { get; set; }

    public Player? Killer { get; set; }
    public Player? Victim { get; set; }
    public Server? Server { get; set; }
}
