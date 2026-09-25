using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// Parses the Half-Life log player-string format into a <see cref="PlayerInfo"/>.
/// </summary>
/// <remarks>
/// <para>
/// The standard format is: <c>"Name&lt;userid&gt;&lt;uniqueid&gt;&lt;team&gt;"</c>
/// with an optional trailing <c>"&lt;role&gt;"</c> segment. The user ID is always
/// a decimal integer; the unique ID is a Steam ID, "BOT", "0", or one of the
/// pending/LAN sentinel values.
/// </para>
/// <para>
/// Bot unique IDs are replaced with a deterministic <c>"BOT:&lt;md5(name+serverAddr)&gt;"</c>
/// to uniquely track individual bots across sessions, matching Perl daemon behaviour.
/// </para>
/// </remarks>
public static partial class PlayerStringParser
{
    // Matches the full player string including optional role segment.
    // Group 1=name, 2=userid, 3=uniqueid, 4=team, 5=role (optional)
    [GeneratedRegex(
        @"^(.*?)<(\d+)><([^<>]*)><([^<>]*)>(?:<([^<>]*)>)?",
        RegexOptions.Singleline)]
    private static partial Regex FullPlayerRegex();

    // Matches the 2-segment form used in some log lines: "Name<uniqueid>"
    [GeneratedRegex(@"^(.+)<([^<>]+)>$")]
    private static partial Regex ShortPlayerRegex();

    /// <summary>
    /// Pending/LAN Steam IDs that should not be resolved to a DB player record yet.
    /// </summary>
    private static readonly HashSet<string> PendingIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNKNOWN", "STEAM_ID_PENDING", "STEAM_ID_LAN",
        "VALVE_ID_PENDING", "VALVE_ID_LAN"
    };

    /// <param name="serverAddr">
    /// The <c>"ip:port"</c> string of the originating server, used to derive a
    /// unique bot identifier that stays stable within a server session.
    /// </param>
    /// <returns>
    /// The parsed <see cref="PlayerInfo"/>, or <see langword="null"/> when the
    /// string is empty or represents the server console.
    /// </returns>
    public static PlayerInfo? Parse(string playerString, string serverAddr)
    {
        if (string.IsNullOrEmpty(playerString)) return null;

        var full = FullPlayerRegex().Match(playerString);
        if (full.Success)
        {
            var name      = full.Groups[1].Value;
            var userIdStr = full.Groups[2].Value;
            var uniqueId  = full.Groups[3].Value;
            var team      = full.Groups[4].Value;
            var role      = full.Groups[5].Value;

            // Console log lines — not a real player
            if (uniqueId == "Console" && team == "Console") return null;

            int.TryParse(userIdStr, out int userId);

            var plainUniqueId = uniqueId;

            // Normalise SteamID3 / strip universe prefix
            uniqueId = SteamIdNormalizer.Normalize(uniqueId);

            bool isBot = BotDetector.IsBot(uniqueId);

            if (isBot)
            {
                // Derive a stable per-bot identity from name+server to keep bots
                // consistent across a session (mirrors Perl md5 approach).
                uniqueId = "BOT:" + BotHash(name, serverAddr);
            }

            return new PlayerInfo
            {
                Name        = name,
                UserId      = userId,
                UniqueId    = uniqueId,
                PlainUniqueId = plainUniqueId,
                Team        = team,
                Role        = role,
                IsBot       = isBot
            };
        }

        // Fallback: "Name<uniqueid>" two-segment form
        var shortMatch = ShortPlayerRegex().Match(playerString);
        if (shortMatch.Success)
        {
            var name     = shortMatch.Groups[1].Value;
            var uniqueId = shortMatch.Groups[2].Value;
            bool isBot   = BotDetector.IsBot(uniqueId);
            if (isBot) uniqueId = "BOT:" + BotHash(name, serverAddr);

            return new PlayerInfo { Name = name, UniqueId = uniqueId, IsBot = isBot };
        }

        return null;
    }

    /// <summary>Returns <see langword="true"/> when the unique ID is a known pending/LAN sentinel.</summary>
    public static bool IsPending(string uniqueId) => PendingIds.Contains(uniqueId);

    // MD5 of "name+serverAddr" gives a compact, stable 32-char hex string per bot.
    private static string BotHash(string name, string serverAddr)
    {
        var input = Encoding.UTF8.GetBytes(name + serverAddr);
        var hash  = MD5.HashData(input);
        return Convert.ToHexStringLower(hash);
    }
}
