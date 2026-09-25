namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Records that a player won a specific award on a specific date.
/// Maps to <c>hlstats_Awards_Players</c>. Multiple rows can exist per
/// (player, award) pair — one per day the award was won.
/// </summary>
public class PlayerAward
{
    public DateTime AwardTime { get; set; }
    public int AwardId { get; set; }
    public int PlayerId { get; set; }
    /// <summary>The value that triggered the award (e.g. number of kills with the weapon that day).</summary>
    public int Count { get; set; }
    public string Game { get; set; } = string.Empty;

    public Player? Player { get; set; }
    public Award? Award { get; set; }
}
