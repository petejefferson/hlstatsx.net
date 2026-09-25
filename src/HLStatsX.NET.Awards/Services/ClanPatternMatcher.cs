using System.Text.RegularExpressions;

namespace HLStatsX.NET.Awards.Services;

internal record ClanPatternMatch(string Tag, string Name);

// Builds and applies clan tag regexes, replicating the Perl hlstats-awards.pl
// DoClans() pattern construction:
//   1. Regex-escape the raw tag pattern
//   2. Wrap the first alphanumeric token in a capture group (extracts clan name)
//   3. Replace literal 'A' with '.' (any-char wildcard) and 'X' with '.?' (optional-char wildcard)
//   4. Wrap the whole thing in an outer capture group and anchor per position
//
// Capture group layout in the final regex:
//   Group 1 = full tag match  (used as clanTag)
//   Group 2 = inner alnum token (used as clanName)
internal sealed class ClanPatternMatcher
{
    private readonly IReadOnlyList<(Regex Pattern, string Position)> _patterns;

    internal ClanPatternMatcher(IEnumerable<(string Pattern, string Position)> tags)
    {
        _patterns = BuildPatterns(tags);
    }

    internal ClanPatternMatch? Match(string playerName)
    {
        foreach (var (pattern, _) in _patterns)
        {
            var m = pattern.Match(playerName);
            if (!m.Success) continue;

            var tag  = m.Groups[1].Value;
            var name = m.Groups.Count > 2 ? m.Groups[2].Value : tag;
            return new ClanPatternMatch(tag, name);
        }
        return null;
    }

    internal static IReadOnlyList<(Regex Pattern, string Position)> BuildPatterns(
        IEnumerable<(string Pattern, string Position)> tags)
    {
        var result = new List<(Regex, string)>();

        foreach (var (pattern, position) in tags)
        {
            var escaped = Regex.Escape(pattern);

            // Wrap only the FIRST alphanumeric sequence in a capture group; this becomes group 2
            // (the group that the Perl code reads as $clanName)
            escaped = new Regex(@"[A-Za-z0-9]+[A-Za-z0-9_\-]*").Replace(escaped, m => $"({m.Value})", 1);

            // Apply Perl wildcard characters: A = any char, X = optional any char
            escaped = escaped.Replace("A", ".").Replace("X", ".?");

            if (position == "START" || position == "EITHER")
                result.Add((new Regex($"^({escaped}).+", RegexOptions.IgnoreCase | RegexOptions.Compiled), position));

            if (position == "END" || position == "EITHER")
                result.Add((new Regex($".+({escaped})$", RegexOptions.IgnoreCase | RegexOptions.Compiled), position));
        }

        return result;
    }
}
