namespace HLStatsX.NET.Awards.Services;

public interface IGeoIpService
{
    Task<GeoIpResult> LookupAsync(CancellationToken ct = default);
}

public record GeoIpResult(int PlayersUpdated, int PlayersSkipped, string DatabasePath);
