namespace HLStatsX.NET.Core.Models;

public record ClanSummaryStats(
    long TotalKills,
    long TotalDeaths,
    long TotalHeadshots,
    long TotalConnectionTime,
    int ActiveMemberCount,
    int TotalMemberCount,
    int AvgSkill,
    double AvgActivity);

public record ClanFavoriteServer(int ServerId, string ServerName);

public record ClanFavoriteWeapon(string WeaponCode, string WeaponName);

public record ClanMemberRow(
    int PlayerId,
    string LastName,
    string? Flag,
    string? Country,
    int Skill,
    int? MmRank,
    int Kills,
    int Deaths,
    int ConnectionTime,
    int ActivityScore,
    double KillDeathRatio,
    double ClanKillPercent,
    string? RankName,
    string? RankImage);

public record ClanWeaponRow(
    string WeaponCode,
    string WeaponName,
    float Modifier,
    long Kills,
    double KillPercent,
    long Headshots,
    double HeadshotPercent,
    double HeadshotsPerKill);

public record ClanMapRow(
    string Map,
    long Kills,
    long Deaths,
    long Headshots,
    double KillPercent,
    double HeadshotPercent,
    double KillDeathRatio,
    double HeadshotsPerKill);

public record ClanActionRow(
    string Code,
    string Description,
    long Count,
    long TotalBonus);

public record ClanTeamRow(
    string TeamName,
    long JoinCount,
    double Percent);

public record ClanRoleRow(
    string RoleCode,
    string RoleName,
    long JoinCount,
    double Percent,
    long Kills,
    long Deaths,
    double KillDeathRatio);

public record ClanWeaponStatsRow(
    string WeaponCode,
    string WeaponName,
    long Shots,
    long Hits,
    long Damage,
    long Headshots,
    long Kills,
    long Deaths,
    double KillDeathRatio,
    double Accuracy,
    double DamagePerHit,
    double ShotsPerKill);

public record ClanWeaponTargetRow(
    string WeaponCode,
    string WeaponName,
    long Hits,
    long Head,
    long Chest,
    long Stomach,
    long LeftArm,
    long RightArm,
    long LeftLeg,
    long RightLeg,
    double LeftPct,
    double MiddlePct,
    double RightPct);

public record ClanMemberLocationRow(
    int PlayerId,
    string LastName,
    int Kills,
    int Deaths,
    float Lat,
    float Lng,
    string? City,
    string? Country);
