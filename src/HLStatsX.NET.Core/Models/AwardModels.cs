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
