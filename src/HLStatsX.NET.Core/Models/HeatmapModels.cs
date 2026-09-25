namespace HLStatsX.NET.Core.Models;

public record HeatPoint(double X, double Y, int Value);

public record HeatmapConfigDto(
    float XOffset,
    float YOffset,
    bool FlipX,
    bool FlipY,
    float Scale
);
