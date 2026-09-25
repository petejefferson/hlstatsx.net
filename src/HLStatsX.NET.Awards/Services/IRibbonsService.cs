namespace HLStatsX.NET.Awards.Services;

public interface IRibbonsService
{
    Task<RibbonsResult> RecalculateAsync(CancellationToken ct = default);
}

public record RibbonsResult(int GamesProcessed, int RibbonsProcessed, int PlayersAwarded);
