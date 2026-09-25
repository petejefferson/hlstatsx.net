namespace HLStatsX.NET.Awards.Services;

public interface IOptimizeService
{
    Task<OptimizeResult> OptimizeAsync(CancellationToken ct = default);
}

public record OptimizeResult(int TablesOptimized);
