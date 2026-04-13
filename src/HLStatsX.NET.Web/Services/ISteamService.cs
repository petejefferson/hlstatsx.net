namespace HLStatsX.NET.Web.Services;

public interface ISteamService
{
    Task<string?> GetAvatarUrlAsync(long steam64, CancellationToken ct = default);

    static long? ToSteam64(string? uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId) || uniqueId.StartsWith("BOT", StringComparison.OrdinalIgnoreCase))
            return null;
        var parts = uniqueId.Split(':');
        if (parts.Length != 2) return null;
        if (!long.TryParse(parts[0], out var y) || !long.TryParse(parts[1], out var z)) return null;
        return 76561197960265728L + y + z * 2;
    }
}
