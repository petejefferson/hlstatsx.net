namespace HLStatsX.NET.Core.Entities;

public class PlayerRibbon
{
    public int PlayerId { get; set; }
    public int RibbonId { get; set; }
    public string Game { get; set; } = string.Empty;

    public Player? Player { get; set; }
    public Ribbon? Ribbon { get; set; }
}
