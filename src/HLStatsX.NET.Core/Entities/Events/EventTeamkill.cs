namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a teamkill event — a player killing a teammate.
/// Teamkills typically result in a skill penalty stored in <see cref="KillerSkillChange"/>.
/// </summary>
public class EventTeamkill
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int KillerId { get; set; }
    public int VictimId { get; set; }
    public string WeaponCode { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
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
