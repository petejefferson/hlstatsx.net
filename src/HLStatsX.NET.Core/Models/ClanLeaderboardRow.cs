namespace HLStatsX.NET.Core.Models;

public record ClanLeaderboardRow
{
    public int ClanId { get; init; }
    public string Name { get; init; } = "";
    public string Tag { get; init; } = "";
    public int MemberCount { get; init; }
    public int TotalKills { get; init; }
    public int TotalDeaths { get; init; }
    public int TotalConnectionTime { get; init; }
    public int AvgSkill { get; init; }
    public double AvgActivity { get; init; }

    public double KillDeathRatio => TotalDeaths == 0
        ? TotalKills
        : Math.Round((double)TotalKills / TotalDeaths, 2);
}
