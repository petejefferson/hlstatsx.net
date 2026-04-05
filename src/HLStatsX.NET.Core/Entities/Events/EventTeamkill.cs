namespace HLStatsX.NET.Core.Entities.Events;

public class EventTeamkill
{
    public long EventId { get; set; }
    public int ServerId { get; set; }
    public int KillerId { get; set; }
    public int VictimId { get; set; }
    public string WeaponCode { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string Game { get; set; } = string.Empty;
    public int KillerSkillChange { get; set; }

    public Player? Killer { get; set; }
    public Player? Victim { get; set; }
    public Server? Server { get; set; }
}
