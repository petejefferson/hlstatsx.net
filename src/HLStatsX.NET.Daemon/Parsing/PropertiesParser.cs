using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// Parses the <c>(key "value")</c> property string appended to many Half-Life log events.
/// </summary>
/// <remarks>
/// Three value formats are handled:
/// <list type="bullet">
///   <item><c>(key "value")</c> — quoted string value</item>
///   <item><c>(key value)</c> — unquoted value (no spaces inside parens)</item>
///   <item><c>(key)</c> — boolean flag; stored as <c>"1"</c></item>
/// </list>
/// Example input: <c>(weapon "ak47") (headshot "1") (attacker_position "-123 456 789")</c>
/// </remarks>
public static partial class PropertiesParser
{
    // Captures: group 1 = key, group 2 = quoted value, group 3 = unquoted value.
    // Key pattern [^\s)]+ stops at whitespace or ')' so it can't greedily consume
    // the closing paren and merge adjacent properties into one match.
    [GeneratedRegex(
        @"\(([^\s)]+)(?:(?: ""(.+?)"")|(?:\s([^\)]+)))?\)",
        RegexOptions.Singleline)]
    private static partial Regex PropertyRegex();

    /// <summary>
    /// Parses all key/value pairs from <paramref name="propString"/> into a dictionary.
    /// Returns an empty dictionary when <paramref name="propString"/> is null or whitespace.
    /// </summary>
    public static Dictionary<string, string> Parse(string? propString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(propString)) return result;

        foreach (Match m in PropertyRegex().Matches(propString))
        {
            var key = m.Groups[1].Value;
            string value;

            if (m.Groups[2].Success)       // quoted
                value = m.Groups[2].Value;
            else if (m.Groups[3].Success)  // unquoted
                value = m.Groups[3].Value;
            else                           // boolean flag
                value = "1";

            // DoDs sends duplicate "player" keys for flag captures — the Perl daemon
            // renames the first to player_a and the second to player_b via a flagindex
            // counter. We replicate: on the second "player" key, promote the stored first
            // to player_a, store the new value as player_b, and remove the "player" key.
            // A third "player" key re-adds as "player" (Perl behaviour for 3+ cappers).
            if (key == "player" && result.ContainsKey("player"))
            {
                if (!result.ContainsKey("player_a"))
                {
                    result["player_a"] = result["player"];
                    result.Remove("player");
                    result["player_b"] = value;
                    continue;
                }
                // Third+ "player": "player" was removed above, falls through to normal add.
            }

            result[key] = value;
        }

        return result;
    }
}
