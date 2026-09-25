using HLStatsX.NET.Core.Entities.Events;

namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Represents a tracked player across all games. One row in <c>hlstats_Players</c>.
/// A player is identified by their <see cref="PlayerId"/>; the same Steam account can
/// have separate rows for each game they play.
/// </summary>
public class Player
{
    public int PlayerId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Game { get; set; } = string.Empty;
    public int? ClanId { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Homepage { get; set; }
    public int Skill { get; set; } = 1000;
    public int LastSkillChange { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Suicides { get; set; }
    public int Headshots { get; set; }
    public int Shots { get; set; }
    public int Hits { get; set; }
    public int Teamkills { get; set; }
    public int ConnectionTime { get; set; }
    public int LastEvent { get; set; }
    public int CreateDate { get; set; }
    /// <summary>Non-zero means the player is hidden from public rankings (effectively a ban).</summary>
    public int HideRanking { get; set; }
    public int BlockAvatar { get; set; }
    /// <summary>Decay-based activity score used for the activity column on leaderboards.</summary>
    public int ActivityScore { get; set; }
    /// <summary>The player's current rank position within their game, cached by the Perl daemon.</summary>
    public int? GameRank { get; set; }
    public int KillStreak { get; set; }
    public int DeathStreak { get; set; }
    /// <summary>Matchmaking rank level reported by some game mods (e.g. CS:GO ranks).</summary>
    public int? MmRank { get; set; }
    public string LastAddress { get; set; } = string.Empty;
    /// <summary>GeoIP-derived state/region of the player's last known IP address.</summary>
    public string State { get; set; } = string.Empty;
    /// <summary>GeoIP-derived latitude of the player's last known IP address.</summary>
    public float? Lat { get; set; }
    /// <summary>GeoIP-derived longitude of the player's last known IP address.</summary>
    public float? Lng { get; set; }

    /// <summary><c>true</c> when the player is hidden from rankings (<see cref="HideRanking"/> != 0).</summary>
    public bool IsHidden   => HideRanking != 0;
    /// <summary><c>true</c> when the player name is the reserved "SourceTV" spectator bot.</summary>
    public bool IsSourceTV => LastName.Equals("SourceTV", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// <c>true</c> when the player is a game bot (their Steam ID starts with "BOT").
    /// SourceTV is excluded from this check — it is handled separately via <see cref="IsSourceTV"/>.
    /// </summary>
    public bool IsBot      => !IsSourceTV && UniqueIds.Any(u => u.UniqueId.StartsWith("BOT", StringComparison.OrdinalIgnoreCase));

    // Navigation properties
    public Clan? Clan { get; set; }
    public Game? GameNavigation { get; set; }
    public ICollection<PlayerName> Names { get; set; } = new List<PlayerName>();
    public ICollection<PlayerUniqueId> UniqueIds { get; set; } = new List<PlayerUniqueId>();
    public ICollection<PlayerHistory> History { get; set; } = new List<PlayerHistory>();
    public ICollection<PlayerAward> Awards { get; set; } = new List<PlayerAward>();
    public ICollection<PlayerRibbon> Ribbons { get; set; } = new List<PlayerRibbon>();
    public ICollection<EventFrag> KillEvents { get; set; } = new List<EventFrag>();
    public ICollection<EventFrag> DeathEvents { get; set; } = new List<EventFrag>();
    public ICollection<EventChat> ChatEvents { get; set; } = new List<EventChat>();

    /// <summary>Kill/death ratio, rounded to 2 decimal places. Returns <see cref="Kills"/> when deaths are zero.</summary>
    public double KillDeathRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
    /// <summary>Headshots as a percentage of total kills, rounded to 1 decimal place.</summary>
    public double HeadshotPercent => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
    /// <summary>Hit accuracy as a percentage of shots fired, rounded to 1 decimal place.</summary>
    public double Accuracy => Shots == 0 ? 0 : Math.Round((double)Hits / Shots * 100, 1);
    /// <summary>Average kills per minute of play time, rounded to 2 decimal places.</summary>
    public double KillsPerMinute => ConnectionTime == 0 ? 0 : Math.Round((double)Kills / (ConnectionTime / 60.0), 2);
}
