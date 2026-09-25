using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// Detects whether a unique ID belongs to a game bot rather than a human player.
/// </summary>
/// <remarks>
/// Mirrors <c>botidcheck()</c> in the Perl daemon. Three patterns identify a bot:
/// <list type="bullet">
///   <item><c>"BOT"</c> — the standard Source Engine bot token.</item>
///   <item><c>"0"</c> — used by some older mods.</item>
///   <item><c>"00000000:N:0"</c> — the "whichbot" plugin format.</item>
/// </list>
/// </remarks>
public static partial class BotDetector
{
    [GeneratedRegex(@"^00000000:\d+:0$")]
    private static partial Regex WhichBotRegex();

    /// <summary>Returns <see langword="true"/> when <paramref name="uniqueId"/> identifies a bot.</summary>
    public static bool IsBot(string uniqueId) =>
        uniqueId is "BOT" or "0" || WhichBotRegex().IsMatch(uniqueId);
}
