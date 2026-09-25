namespace HLStatsX.NET.Awards.Services;

public interface IAwardsCalculationService
{
    Task<AwardsCalculationResult> CalculateAsync(CancellationToken ct = default);
    Task<AwardsPreviewResult> PreviewAsync(DateOnly? date = null, CancellationToken ct = default);
}

public record AwardsCalculationResult(int AwardsProcessed, string AwardsDate);

public record AwardsPreviewRow(
    string Game,
    string AwardCode,
    string AwardName,
    string AwardType,
    int? DailyWinnerPlayerId,
    string? DailyWinnerName,
    int? DailyWinnerCount);

public record AwardsPreviewResult(string AwardsDate, IReadOnlyList<AwardsPreviewRow> Rows);
