namespace HLStatsX.NET.Core.Models;

public record RealStats(
    long RealKills, long RealDeaths, long RealHeadshots, long RealTeamkills,
    double RealKpd, double RealHpk);

public record PingStats(int AvgPing, int AvgLatency);

public record FavoriteServer(int ServerId, string ServerName);

public record FavoriteWeapon(string WeaponCode, string WeaponName);

public record KillStatRow(
    int VictimId, string VictimName,
    long Kills, long Deaths, long Headshots);

public record MapStatRow(
    string Map, long Kills, long Deaths, long Headshots);

public record ServerStatRow(
    int ServerId, string ServerName, long Kills, long Deaths, long Headshots);

public record WeaponStatRow(
    string WeaponCode, string WeaponName, float Modifier, long Kills, long Headshots);

public record TeamStatRow(
    string TeamCode, string TeamName, int JoinCount, int TotalJoins);

public record RoleStatRow(
    string RoleCode, string? RoleImage, int JoinCount, int TotalJoins, long Kills, long Deaths);

public record RibbonDisplay(
    int RibbonId, string RibbonName, string? Image, bool Earned);

public record ActionStatRow(
    string Description, long Count, double AccumulatedPoints);
