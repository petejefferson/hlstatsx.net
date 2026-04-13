namespace HLStatsX.NET.Core.Models;

public record CountryLeaderboardRow
{
    public string Flag { get; init; } = "";
    public string Name { get; init; } = "";
    public int MemberCount { get; init; }
    public long TotalKills { get; init; }
    public long TotalDeaths { get; init; }
    public long TotalConnectionTime { get; init; }
    public int AvgSkill { get; init; }
    public double AvgActivity { get; init; }

    public double KillDeathRatio => TotalDeaths == 0
        ? TotalKills
        : Math.Round((double)TotalKills / TotalDeaths, 2);
}
