namespace HLStatsX.NET.Awards.Services;

public interface IClansService
{
    Task<ClansResult> RecalculateAsync(CancellationToken ct = default);
}

public record ClansResult(int PlayersProcessed, int ClansCreated, int PlayersUpdated, int PlayersCleared);
