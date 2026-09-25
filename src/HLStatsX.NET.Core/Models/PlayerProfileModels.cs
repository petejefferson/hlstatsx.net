namespace HLStatsX.NET.Core.Models;

/// <summary>Raw server-side kill/death/headshot totals, read directly from the event tables.</summary>
public record RealStats(
    long RealKills, long RealDeaths, long RealHeadshots, long RealTeamkills,
    double RealKpd, double RealHpk);

/// <summary>Average ping (ms) and packet-loss latency for a player, from <c>hlstats_Events_Latency</c>.</summary>
public record PingStats(int AvgPing, int AvgLatency);

/// <summary>The server a player has spent the most time on.</summary>
public record FavoriteServer(int ServerId, string ServerName);

/// <summary>The weapon a player has used most, by kill count.</summary>
public record FavoriteWeapon(string WeaponCode, string WeaponName);

/// <summary>Head-to-head kill/death record between the profiled player and one opponent.</summary>
public record KillStatRow(
    int VictimId, string VictimName,
    long Kills, long Deaths, long Headshots,
    bool VictimIsBot = false, string? VictimFlag = null);

/// <summary>Kill/death/headshot totals for a single map on a player profile.</summary>
public record MapStatRow(
    string Map, long Kills, long Deaths, long Headshots);

/// <summary>Kill/death/headshot totals for a single server on a player profile.</summary>
public record ServerStatRow(
    int ServerId, string ServerName, long Kills, long Deaths, long Headshots);

/// <summary>Aggregated weapon usage stats for a player (kills and headshots per weapon).</summary>
public record WeaponStatRow(
    string WeaponCode, string WeaponName, float Modifier, long Kills, long Headshots, int? WeaponId = null);

/// <summary>
/// Detailed weapon accuracy stats sourced from <c>hlstats_Events_Statsme</c> (the game's
/// built-in statsme plugin). Includes shots, hits, damage, and derived ratios.
/// </summary>
public record WeaponStatsmeRow(
    string WeaponCode, string WeaponName, int? WeaponId,
    long Shots, long Hits, long Damage, long Headshots, long Kills, long Deaths,
    double Kdr, double Accuracy, double DamagePerHit, double ShotsPerKill);

/// <summary>
/// Per-hitbox breakdown for a weapon, showing how many hits landed on each body part.
/// Left/Middle/Right percentages are zone groupings (limbs / torso / limbs).
/// </summary>
public record WeaponTargetRow(
    string WeaponCode, string WeaponName, int? WeaponId,
    long Hits, long Head, long Chest, long Stomach,
    long LeftArm, long RightArm, long LeftLeg, long RightLeg,
    double LeftPct, double MiddlePct, double RightPct);

/// <summary>How many times a player joined a specific team, as a fraction of all team-change events.</summary>
public record TeamStatRow(
    string TeamCode, string TeamName, int JoinCount, int TotalJoins);

/// <summary>How many times a player played a specific role, plus their kill/death record in that role.</summary>
public record RoleStatRow(
    string RoleCode, string RoleName, string? RoleImage, int JoinCount, int TotalJoins, long Kills, long Deaths);

/// <summary>A ribbon and whether the profiled player has earned it yet.</summary>
public record RibbonDisplay(
    int RibbonId, string RibbonName, string? Image, bool Earned);

/// <summary>Aggregated in-game action totals for a player (e.g. bomb plants, flag captures).</summary>
public record ActionStatRow(
    string Description, long Count, double AccumulatedPoints);

/// <summary>A single point on the player skill trend chart — one per session.</summary>
public record TrendPoint(DateTime EventTime, int Skill, int SkillChange);

/// <summary>Per-session summary row shown on the player Sessions tab.</summary>
public record PlayerSessionRow(
    DateTime EventTime,
    int SkillChange,
    int Skill,
    int ConnectionTime,
    int Kills,
    int Deaths,
    double Kpd,
    int Headshots,
    double Hpk,
    int Suicides,
    int TeamKills,
    int KillStreak);

/// <summary>An award the profiled player currently holds globally (all-time record).</summary>
public record GlobalAwardRow(string AwardType, string Code, string Name);

/// <summary>A row on the player awards sub-page — either a summary (total wins) or a single win detail.</summary>
public record PlayerAwardRow(int AwardId, string Name, string Verb, DateTime AwardTime, int Count);

/// <summary>A single event row on the player event history page.</summary>
public record PlayerEventRow(
    DateTime EventTime,
    string EventType,
    string Description,
    string ServerName,
    string Map);
