using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// Parses raw UDP payloads from Half-Life game servers into structured log lines.
/// </summary>
/// <remarks>
/// HL2 log format: <c>L MM/DD/YYYY - HH:MM:SS: &lt;event text&gt;</c><br/>
/// The leading <c>L </c> may be preceded by arbitrary bytes (e.g. the 4-byte
/// <c>0xFF 0xFF 0xFF 0xFF</c> Source Engine header or proxy cruft) — the regex
/// skips everything up to the first <c>L </c> that precedes a valid timestamp,
/// matching the Perl daemon's exploit-fix pattern <c>/^(?:.*?)?L …/</c>.
/// </remarks>
public static partial class LogLineParser
{
    // Source-generated: avoids per-call Regex allocation and JITs the DFA once.
    // Captures: 1=month 2=day 3=year 4=hour 5=min 6=sec
    [GeneratedRegex(
        @"(?:.*?)?L (\d{2})/(\d{2})/(\d{4}) - (\d{2}):(\d{2}):(\d{2}):\s*",
        RegexOptions.Singleline)]
    private static partial Regex TimestampRegex();

    /// <summary>
    /// Attempts to parse a raw server log payload.
    /// </summary>
    /// <param name="raw">The UTF-8 decoded UDP payload.</param>
    /// <param name="result">
    /// When successful, contains the parsed timestamp and event text.
    /// </param>
    /// <returns><see langword="true"/> if the payload contained a valid log line.</returns>
    public static bool TryParse(string raw, out ParsedLogLine result)
    {
        // Strip noise characters the Perl daemon also strips
        var cleaned = StripNoise(raw);

        var match = TimestampRegex().Match(cleaned);
        if (!match.Success)
        {
            result = default;
            return false;
        }

        int month  = int.Parse(match.Groups[1].ValueSpan);
        int day    = int.Parse(match.Groups[2].ValueSpan);
        int year   = int.Parse(match.Groups[3].ValueSpan);
        int hour   = int.Parse(match.Groups[4].ValueSpan);
        int minute = int.Parse(match.Groups[5].ValueSpan);
        int second = int.Parse(match.Groups[6].ValueSpan);

        // Perl uses timelocal() which treats the log's time components as LOCAL time.
        // DateTimeKind.Local on the intermediate DateTime ensures DateTimeOffset picks up
        // the correct UTC offset (including DST), matching Perl's Unix timestamp.
        var timestamp = new DateTimeOffset(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local));
        var eventText = cleaned[(match.Index + match.Length)..];

        result = new ParsedLogLine { Timestamp = timestamp, EventText = eventText };
        return true;
    }

    // The Perl daemon strips \r \n \0 and three in-engine tag strings.
    private static string StripNoise(string raw)
    {
        if (raw.Length == 0) return raw;

        // Fast path: no control chars or known tags → skip allocation
        bool hasNoise = false;
        foreach (char c in raw)
        {
            if (c is '\r' or '\n' or '\0') { hasNoise = true; break; }
        }
        if (!hasNoise && !raw.Contains("[No.C-D]", StringComparison.Ordinal)
                      && !raw.Contains("[OLD.C-D]", StringComparison.Ordinal)
                      && !raw.Contains("[NOCL]", StringComparison.Ordinal))
            return raw;

        return raw
            .Replace("\r", "").Replace("\n", "").Replace("\0", "")
            .Replace("[No.C-D]", "")
            .Replace("[OLD.C-D]", "")
            .Replace("[NOCL]", "");
    }
}
