namespace HLStatsX.NET.Core.Entities;

public class Rank
{
    public int RankId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int MinKills { get; set; }
    public int MaxKills { get; set; }
}
