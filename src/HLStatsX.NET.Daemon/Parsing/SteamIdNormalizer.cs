using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// Normalises Steam ID strings to the <c>"Y:Z"</c> storage format used throughout
/// <c>hlstats_PlayerUniqueIds</c>.
/// </summary>
/// <remarks>
/// The Perl daemon performs two substitutions in order:
/// <list type="number">
///   <item>
///     Convert the modern SteamID3 format <c>[U:1:N]</c> to legacy SteamID2:
///     <c>STEAM_0:&lt;N%2&gt;:&lt;N/2&gt;</c>
///   </item>
///   <item>Strip the <c>STEAM_X:</c> universe prefix, leaving just <c>"Y:Z"</c>.</item>
/// </list>
/// Result examples: <c>"STEAM_0:1:12345"</c> → <c>"1:12345"</c>,
/// <c>"[U:1:24691]"</c> → <c>"1:12345"</c>.
/// </remarks>
public static partial class SteamIdNormalizer
{
    // Matches the SteamID3 format [U:1:N]
    [GeneratedRegex(@"\[U:1:(\d+)\]")]
    private static partial Regex SteamId3Regex();

    // Matches the STEAM_X: universe prefix to strip
    [GeneratedRegex(@"^STEAM_\d+:")]
    private static partial Regex UniversePrefixRegex();

    /// <summary>
    /// Normalises a raw unique ID from the log to the <c>"Y:Z"</c> storage format.
    /// Non-Steam IDs (BOT, UNKNOWN, LAN) are returned unchanged.
    /// </summary>
    public static string Normalize(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return uniqueId;

        // Step 1: [U:1:N] → STEAM_0:Y:Z
        var result = SteamId3Regex().Replace(uniqueId, m =>
        {
            long n = long.Parse(m.Groups[1].ValueSpan);
            return $"STEAM_0:{n % 2}:{n / 2}";
        });

        // Step 2: Strip STEAM_X: universe prefix → Y:Z
        result = UniversePrefixRegex().Replace(result, "");

        return result;
    }
}
