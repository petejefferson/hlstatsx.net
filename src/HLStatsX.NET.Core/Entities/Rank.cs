namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Defines a named rank tier awarded to players whose kill count falls within a range.
/// Maps to <c>hlstats_Ranks</c>. Rank images are stored in <c>wwwroot/images/ranks/</c>.
/// </summary>
public class Rank
{
    public int RankId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int MinKills { get; set; }
    public int MaxKills { get; set; }
}
