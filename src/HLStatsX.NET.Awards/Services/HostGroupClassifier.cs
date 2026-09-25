using System.Text.RegularExpressions;

namespace HLStatsX.NET.Awards.Services;

/// <summary>
/// Classifies a resolved hostname into a host group name, replicating the Perl
/// <c>getHostGroup()</c> function from <c>HLstats.plib</c>.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
///   <item>Check caller-supplied <c>hlstats_HostGroups</c> patterns (longest first).
///   Shell-style <c>*</c> glob expands to <c>[^.]*</c> (no dot-crossing).</item>
///   <item>If no custom pattern matches, extract the last 2–3 domain parts using
///   the same heuristics as the Perl original.</item>
/// </list>
/// </remarks>
public sealed class HostGroupClassifier
{
    // Country codes that don't use categorical second-level domains.
    // e.g. Germany: 'max.isp.de' → 'isp.de' (2 parts), not 'xyz.isp.de' (3 parts).
    private static readonly HashSet<string> NoSldCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "ca", "ch", "be", "de", "ee", "es", "fi", "fr", "ie", "nl", "no", "ru", "se"
    };

    private static readonly string NoSldAlternation =
        string.Join("|", NoSldCountries.OrderBy(c => c));

    private readonly IReadOnlyList<(string Pattern, string Name)> _patterns;

    /// <param name="patterns">
    /// Host group patterns loaded from <c>hlstats_HostGroups</c>, ordered longest-first
    /// then alphabetically (mirrors the Perl <c>queryHostGroups</c> ORDER BY).
    /// </param>
    public HostGroupClassifier(IReadOnlyList<(string Pattern, string Name)> patterns)
    {
        _patterns = patterns;
    }

    /// <summary>
    /// Returns the host group name for <paramref name="hostname"/>, or an empty string
    /// when <paramref name="hostname"/> is empty.
    /// </summary>
    public string Classify(string hostname)
    {
        if (string.IsNullOrEmpty(hostname))
            return string.Empty;

        foreach (var (pattern, name) in _patterns)
        {
            // Shell glob: * matches any sequence of non-dot chars
            var regexPattern = Regex.Escape(pattern).Replace("\\*", "[^.]*") + "$";
            if (Regex.IsMatch(hostname, regexPattern, RegexOptions.IgnoreCase))
                return name;
        }

        return ExtractDomainGroup(hostname);
    }

    /// <summary>
    /// Extracts the last 2 or 3 domain parts from a hostname using the same heuristic
    /// as the Perl <c>getHostGroup</c> fallback.
    /// </summary>
    internal static string ExtractDomainGroup(string hostname)
    {
        // Pattern 1: last two parts when TLD is a no-SLD country code or a 3-letter gTLD
        // e.g. 'host.isp.net'  → 'isp.net'
        //      'host.isp.de'   → 'isp.de'
        var pattern1 = @"([\w-]+\.(?:" + NoSldAlternation + @"|\w{3}))$";
        var m1 = Regex.Match(hostname, pattern1, RegexOptions.IgnoreCase);
        if (m1.Success)
            return m1.Groups[1].Value;

        // Pattern 2: last three parts when TLD is a 2-letter ccTLD that uses SLDs
        // e.g. 'host.isp.co.uk' → 'isp.co.uk'
        var m2 = Regex.Match(hostname, @"([\w-]+\.[\w-]+\.\w{2})$", RegexOptions.IgnoreCase);
        if (m2.Success)
            return m2.Groups[1].Value;

        return hostname;
    }
}
