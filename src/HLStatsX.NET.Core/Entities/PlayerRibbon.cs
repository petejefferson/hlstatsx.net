namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Records that a player has earned a ribbon. Maps to <c>hlstats_Ribbons_Players</c>.
/// A player can only earn each ribbon once.
/// </summary>
public class PlayerRibbon
{
    public int PlayerId { get; set; }
    public int RibbonId { get; set; }
    public string Game { get; set; } = string.Empty;

    public Player? Player { get; set; }
    public Ribbon? Ribbon { get; set; }
}
