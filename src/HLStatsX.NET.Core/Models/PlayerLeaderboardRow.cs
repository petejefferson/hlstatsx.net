using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

/// <summary>
/// Flattened row used on the player leaderboard. It unifies both the all-time ranking
/// (sourced from <c>hlstats_Players</c>) and period rankings (sourced from
/// <c>hlstats_Players_History</c> aggregates) so a single view template handles both.
/// </summary>
public record PlayerLeaderboardRow
{
    public int PlayerId { get; init; }
    public string LastName { get; init; } = "";
    public string? Flag { get; init; }
    public string? Country { get; init; }
    public Clan? Clan { get; init; }
    public int ActivityScore { get; init; }
    /// <summary>
    /// Career kill total, always sourced from the live <c>hlstats_Players</c> table.
    /// Used for the army-rank image which must reflect overall progress, not a single period.
    /// </summary>
    public int AllTimeKills { get; init; }
    /// <summary>
    /// The sortable points value for this row. For the total ranking this is the player's
    /// current skill rating; for period rankings it is the sum of <c>skill_change</c> during the period.
    /// </summary>
    public int Points { get; init; }
    public int LastSkillChange { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Headshots { get; init; }
    public int ConnectionTime { get; init; }
    public int Shots { get; init; }
    public int Hits { get; init; }
    public bool IsBot { get; init; }

    public double KillDeathRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
    public double HsPerKill      => Kills   == 0 ? 0    : Math.Round((double)Headshots / Kills, 2);
    public double Accuracy       => Shots   == 0 ? 0    : Math.Round((double)Hits / Shots * 100, 1);
}
