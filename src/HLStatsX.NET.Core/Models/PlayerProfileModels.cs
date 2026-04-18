namespace HLStatsX.NET.Core.Models;

public record RealStats(
    long RealKills, long RealDeaths, long RealHeadshots, long RealTeamkills,
    double RealKpd, double RealHpk);

public record PingStats(int AvgPing, int AvgLatency);

public record FavoriteServer(int ServerId, string ServerName);

public record FavoriteWeapon(string WeaponCode, string WeaponName);

public record KillStatRow(
    int VictimId, string VictimName,
    long Kills, long Deaths, long Headshots,
    bool VictimIsBot = false);

public record MapStatRow(
    string Map, long Kills, long Deaths, long Headshots);

public record ServerStatRow(
    int ServerId, string ServerName, long Kills, long Deaths, long Headshots);

public record WeaponStatRow(
    string WeaponCode, string WeaponName, float Modifier, long Kills, long Headshots, int? WeaponId = null);

public record WeaponStatsmeRow(
    string WeaponCode, string WeaponName, int? WeaponId,
    long Shots, long Hits, long Damage, long Headshots, long Kills, long Deaths,
    double Kdr, double Accuracy, double DamagePerHit, double ShotsPerKill);

public record WeaponTargetRow(
    string WeaponCode, string WeaponName, int? WeaponId,
    long Hits, long Head, long Chest, long Stomach,
    long LeftArm, long RightArm, long LeftLeg, long RightLeg,
    double LeftPct, double MiddlePct, double RightPct);

public record TeamStatRow(
    string TeamCode, string TeamName, int JoinCount, int TotalJoins);

public record RoleStatRow(
    string RoleCode, string RoleName, string? RoleImage, int JoinCount, int TotalJoins, long Kills, long Deaths);

public record RibbonDisplay(
    int RibbonId, string RibbonName, string? Image, bool Earned);

public record ActionStatRow(
    string Description, long Count, double AccumulatedPoints);

public record TrendPoint(DateTime EventTime, int Skill, int SkillChange);

public record GlobalAwardRow(string AwardType, string Code, string Name);
