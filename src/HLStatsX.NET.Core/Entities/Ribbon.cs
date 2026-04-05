namespace HLStatsX.NET.Core.Entities;

public class Ribbon
{
    public int RibbonId { get; set; }
    public string? AwardCode { get; set; }
    public int AwardCount { get; set; }
    public int Special { get; set; }
    public string Game { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string RibbonName { get; set; } = string.Empty;

    public ICollection<PlayerRibbon> PlayerRibbons { get; set; } = new List<PlayerRibbon>();
}
