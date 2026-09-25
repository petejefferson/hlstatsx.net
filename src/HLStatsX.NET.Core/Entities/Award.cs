namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Represents an award that can be won by players. Maps to <c>hlstats_Awards</c>.
/// Awards come in two flavours: daily (most of something on a given day) and global
/// (all-time record holder). The <see cref="AwardType"/> field distinguishes them.
/// </summary>
public class Award
{
    public int AwardId { get; set; }
    /// <summary>
    /// Category of this award. Common values are <c>"W"</c> (weapon kills),
    /// <c>"K"</c> (kill action), or <c>"D"</c> (death/damage action).
    /// </summary>
    public string AwardType { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    /// <summary>Internal code linking the award to a weapon or action, e.g. <c>"ak47"</c>.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Human-readable description of the achievement, e.g. <c>"killed the most players with"</c>.</summary>
    public string Verb { get; set; } = string.Empty;
    public int? DailyWinnerId { get; set; }
    public int? DailyWinnerCount { get; set; }
    public int? GlobalWinnerId { get; set; }
    public int? GlobalWinnerCount { get; set; }

    public Player? DailyWinner { get; set; }
    public Player? GlobalWinner { get; set; }

    public ICollection<PlayerAward> PlayerAwards { get; set; } = new List<PlayerAward>();
}
