namespace HLStatsX.NET.Core.Entities;

public class PlayerName
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LastUse { get; set; }
    public int Numuses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int ConnectionTime { get; set; }
    public int Headshots { get; set; }
    public int Suicides { get; set; }
    public int Shots { get; set; }
    public int Hits { get; set; }

    public Player? Player { get; set; }

    public double KdRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);
    public double HsKRatio => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills, 2);
    public double Accuracy => Shots == 0 ? 0 : Math.Round((double)Hits / Shots * 100, 1);
}
