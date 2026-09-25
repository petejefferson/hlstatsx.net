namespace HLStatsX.NET.Core.Entities;

public class HeatmapConfig
{
    public int Id { get; set; }
    public string Map { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public float XOffset { get; set; }
    public float YOffset { get; set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public bool Rotate { get; set; }
    public int Days { get; set; }
    public float Scale { get; set; }
}
