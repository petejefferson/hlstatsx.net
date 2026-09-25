namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// An achievement badge (ribbon) awarded to players who meet a specific criterion,
/// such as earning a daily award a set number of times. Maps to <c>hlstats_Ribbons</c>.
/// </summary>
public class Ribbon
{
    public int RibbonId { get; set; }
    /// <summary>Award code this ribbon is linked to (matches <see cref="Award.Code"/>), if any.</summary>
    public string? AwardCode { get; set; }
    /// <summary>How many times the linked award must be won for this ribbon to be earned.</summary>
    public int AwardCount { get; set; }
    /// <summary>When non-zero, this ribbon is special/one-of-a-kind (e.g. founder ribbon).</summary>
    public int Special { get; set; }
    public string Game { get; set; } = string.Empty;
    /// <summary>Image filename within <c>wwwroot/images/ribbons/</c>.</summary>
    public string? Image { get; set; }
    public string RibbonName { get; set; } = string.Empty;

    public ICollection<PlayerRibbon> PlayerRibbons { get; set; } = new List<PlayerRibbon>();
}
