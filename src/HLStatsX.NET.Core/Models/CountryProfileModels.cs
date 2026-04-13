namespace HLStatsX.NET.Core.Models;

public record CountryProfile
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

    public double KillsPerMinute => TotalConnectionTime == 0
        ? 0
        : Math.Round((double)TotalKills / (TotalConnectionTime / 60.0), 2);

    public long AvgKills => MemberCount == 0 ? 0 : TotalKills / MemberCount;

    public long AvgConnectionTime => MemberCount == 0 ? 0 : TotalConnectionTime / MemberCount;
}

public record CountryMember
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = "";
    public string? Flag { get; init; }
    public string? Country { get; init; }
    public int Skill { get; init; }
    public int? MmRank { get; init; }
    public double Activity { get; init; }
    public int ConnectionTime { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public double KillPercent { get; init; }

    public double KillDeathRatio => Deaths == 0
        ? Kills
        : Math.Round((double)Kills / Deaths, 2);
}
