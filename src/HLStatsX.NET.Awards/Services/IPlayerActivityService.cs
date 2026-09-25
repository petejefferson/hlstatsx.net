namespace HLStatsX.NET.Awards.Services;

public interface IPlayerActivityService
{
    Task<PlayerActivityResult> UpdateAsync(CancellationToken ct = default);
}

public record PlayerActivityResult(int MinActivityDays, bool UsedTimestamp);
