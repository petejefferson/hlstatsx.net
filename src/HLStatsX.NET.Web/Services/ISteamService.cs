namespace HLStatsX.NET.Web.Services;

/// <summary>
/// Retrieves Steam profile data (avatar URLs) for players via the Steam Community XML API.
/// Responses are cached in memory for 24 hours to avoid hammering the Steam API.
/// </summary>
public interface ISteamService
{
    /// <summary>
    /// Returns the full-size avatar URL for the given Steam64 ID, or <c>null</c> if
    /// the Steam API is unavailable or the profile cannot be found.
    /// </summary>
    Task<string?> GetAvatarUrlAsync(long steam64, CancellationToken ct = default);

    /// <summary>
    /// Converts a HLStatsX unique ID string (in <c>"Y:Z"</c> format, matching
    /// the <c>STEAM_X:Y:Z</c> legacy format) to a Steam64 ID.
    /// Returns <c>null</c> for bots (IDs starting with "BOT") or malformed strings.
    /// </summary>
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
