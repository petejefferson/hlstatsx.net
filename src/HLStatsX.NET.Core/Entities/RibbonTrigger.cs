namespace HLStatsX.NET.Core.Entities;

public class RibbonTrigger
{
    public int TriggerId { get; set; }
    public int RibbonId { get; set; }
    public string? TriggerType { get; set; }
    public string? TriggerCode { get; set; }
    public int TriggerValue { get; set; }

    public Ribbon? Ribbon { get; set; }
}
