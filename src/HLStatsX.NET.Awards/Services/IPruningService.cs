namespace HLStatsX.NET.Awards.Services;

public interface IPruningService
{
    Task<PruningResult> PruneAsync(CancellationToken ct = default);
}

public record PruningResult(
    int DeleteDays,
    long EventRowsDeleted,
    long HistoryRowsDeleted,
    long TrendRowsDeleted,
    long ServerLoadRowsDeleted);
