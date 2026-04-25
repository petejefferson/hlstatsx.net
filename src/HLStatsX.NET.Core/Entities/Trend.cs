namespace HLStatsX.NET.Core.Entities;

public class Trend
{
    public int Timestamp { get; set; }
    public string Game { get; set; } = string.Empty;
    public int Players { get; set; }
    public int Kills { get; set; }
    public int Headshots { get; set; }
    public int Servers { get; set; }
    public int ActSlots { get; set; }
    public int MaxSlots { get; set; }
}
