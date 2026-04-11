using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

public record PlayerLeaderboardRow
{
    public int PlayerId { get; init; }
    public string LastName { get; init; } = "";
    public string? Flag { get; init; }
    public string? Country { get; init; }
    public Clan? Clan { get; init; }
    public int ActivityScore { get; init; }
    public int AllTimeKills { get; init; }   // used for army rank — always the career total
    public int Points { get; init; }          // skill (total ranking) or sum of skill_change (period)
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Headshots { get; init; }
    public int ConnectionTime { get; init; }
    public int Shots { get; init; }
    public int Hits { get; init; }

    public double KillDeathRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
    public double HsPerKill      => Kills   == 0 ? 0    : Math.Round((double)Headshots / Kills, 2);
    public double Accuracy       => Shots   == 0 ? 0    : Math.Round((double)Hits / Shots * 100, 1);
}
