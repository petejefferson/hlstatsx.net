namespace HLStatsX.NET.Core.Entities;

public class MapCount
{
    public int RowId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Headshots { get; set; }
}
