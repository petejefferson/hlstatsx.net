namespace HLStatsX.NET.Core.Entities;

public class Team
{
    public int TeamId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PlayerlistBgcolor { get; set; }
    public string? PlayerlistColor { get; set; }
    public int PlayerlistIndex { get; set; }

    public Game? GameNavigation { get; set; }
}
