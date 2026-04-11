using HLStatsX.NET.Core.Entities.Events;

namespace HLStatsX.NET.Core.Entities;

public class Player
{
    public int PlayerId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Game { get; set; } = string.Empty;
    public int? ClanId { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Flag { get; set; }
    public string? Email { get; set; }
    public string? Homepage { get; set; }
    public int Skill { get; set; } = 1000;
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
    public int HideRanking { get; set; }
    public int ActivityScore { get; set; }
    public int? GameRank { get; set; }
    public int KillStreak { get; set; }
    public int DeathStreak { get; set; }
    public int? MmRank { get; set; }
    public float? Lat { get; set; }
    public float? Lng { get; set; }

    public bool IsHidden => HideRanking != 0;

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

    public double KillDeathRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
    public double HeadshotPercent => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
    public double Accuracy => Shots == 0 ? 0 : Math.Round((double)Hits / Shots * 100, 1);
    public double KillsPerMinute => ConnectionTime == 0 ? 0 : Math.Round((double)Kills / (ConnectionTime / 60.0), 2);
}
