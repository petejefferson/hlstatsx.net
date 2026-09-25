namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// A snapshot of a player's statistics at the end of a play session.
/// Maps to <c>hlstats_Players_History</c>. The Perl daemon writes one row per
/// disconnect event, capturing how stats changed during that session.
/// </summary>
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
    /// <summary>
    /// Change in skill points during this session. Negative means the player lost points.
    /// </summary>
    public int SkillChange { get; set; }
    public int Suicides { get; set; }
    public int TeamKills { get; set; }
    public int KillStreak { get; set; }
    public int DeathStreak { get; set; }

    public Player? Player { get; set; }
}
