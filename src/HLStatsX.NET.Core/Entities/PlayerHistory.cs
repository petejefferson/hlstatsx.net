namespace HLStatsX.NET.Core.Entities;

public class PlayerHistory
{
    public int PlayerId { get; set; }
    public DateTime EventTime { get; set; }
    public string Game { get; set; } = string.Empty;
    public int Skill { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Headshots { get; set; }
    public int ConnectionTime { get; set; }
    public int SkillChange { get; set; }
    public int Suicides { get; set; }
    public int TeamKills { get; set; }
    public int KillStreak { get; set; }

    public Player? Player { get; set; }
}
