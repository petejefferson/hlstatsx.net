using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Models;

public record RankRow(Rank Rank, int PlayerCount);

public record RibbonRow(Ribbon Ribbon, int AchievedCount, string? AwardName);

public record DailyAwardHistoryRow(
    int PlayerId,
    DateTime AwardTime,
    string PlayerName,
    string? Flag,
    int Count);

public record RankPlayerRow(int PlayerId, string PlayerName, string? Flag, int Kills, int Skill);

public record RibbonDetailRow(int PlayerId, string PlayerName, string? Flag, int NumAwards, string? AwardName);
