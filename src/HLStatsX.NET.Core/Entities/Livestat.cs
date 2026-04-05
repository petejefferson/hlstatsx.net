namespace HLStatsX.NET.Core.Entities;

public class Livestat
{
    public int PlayerId { get; set; }
    public int ServerId { get; set; }
    public string CliAddress { get; set; } = string.Empty;
    public string CliCity { get; set; } = string.Empty;
    public string CliCountry { get; set; } = string.Empty;
    public string CliFlag { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Suicides { get; set; }
    public int Headshots { get; set; }
    public int Shots { get; set; }
    public int Hits { get; set; }
    public bool IsDead { get; set; }
    public int Ping { get; set; }
    public int Connected { get; set; }
    public int SkillChange { get; set; }
    public int Skill { get; set; }

    public Server? Server { get; set; }

    public double HeadshotPercent => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
    public double Accuracy => Shots == 0 ? 0 : Math.Round((double)Hits / Shots * 100, 1);
    public string ConnectedFormatted
    {
        get
        {
            if (Connected <= 0) return "Unknown";
            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Connected;
            if (elapsed < 0) elapsed = 0;
            return TimeSpan.FromSeconds(elapsed).ToString(@"hh\:mm\:ss");
        }
    }
}
