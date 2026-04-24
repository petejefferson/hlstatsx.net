namespace HLStatsX.NET.Core.Models;

public record ActionListRow(string Code, string Description, int Count, int RewardPlayer);

public record ActionAchieverRow(int PlayerId, string PlayerName, string? Flag, long Count, long SkillBonusTotal);

public record ActionVictimRow(int VictimId, string PlayerName, string? Flag, long Count, long SkillBonusTotal);
