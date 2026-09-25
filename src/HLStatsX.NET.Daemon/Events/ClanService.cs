using System.Text.RegularExpressions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Matches a player's name against configured clan tag patterns and returns the
/// clan ID, creating the clan row if it does not yet exist.
/// Mirrors <c>getClanId</c> in <c>hlstats.pl</c>.
/// </summary>
public sealed class ClanService
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<ClanService> _logger;

    public ClanService(IDbContextFactory<HLStatsDbContext> dbFactory, ILogger<ClanService> logger)
    {
        _dbFactory = dbFactory;
        _logger    = logger;
    }

    /// <summary>
    /// Returns the clan ID whose tag matches <paramref name="playerName"/> in
    /// <paramref name="game"/>, or <see langword="null"/> if no tag matches.
    /// Creates the <c>hlstats_Clans</c> row on first match for a new tag.
    /// </summary>
    public async Task<int?> MatchAsync(string playerName, string game, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Load tags longest-first (mirrors Perl ORDER BY LENGTH(pattern) DESC, id)
        var tags = await db.ClanTags
            .OrderByDescending(t => t.Pattern.Length)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        foreach (var tag in tags)
        {
            var (matchedTag, matchedName) = TryMatchTag(tag, playerName);
            if (matchedTag is null) continue;

            // Look up existing clan by tag + game
            var clan = await db.Clans
                .SingleOrDefaultAsync(c => c.Tag == matchedTag && c.Game == game, ct);

            if (clan is not null)
                return clan.ClanId;

            // New clan — create it (mirrors Perl REPLACE INTO hlstats_Clans)
            clan = new Clan
            {
                Tag  = matchedTag,
                Name = matchedName ?? matchedTag,
                Game = game,
            };
            db.Clans.Add(clan);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created clan \"{Name}\" (id={Id}) with tag \"{Tag}\" for player \"{Player}\".",
                clan.Name, clan.ClanId, clan.Tag, playerName);

            return clan.ClanId;
        }

        return null;
    }

    /// <summary>
    /// Converts a Perl-style clan tag pattern to a C# regex and tries to match
    /// <paramref name="name"/>. Returns the matched tag text and inner clan name,
    /// or <see langword="null"/> on no match.
    /// </summary>
    internal static (string? tag, string? clanName) TryMatchTag(ClanTag tag, string name)
    {
        // Step 1: Regex.Escape mirrors Perl quotemeta
        string escaped = Regex.Escape(tag.Pattern);

        // Step 2: wrap the FIRST alphanumeric word in a capture group (mirrors Perl s///)
        // This gives group 2 = inner word (the clan name portion).
        // Instance Replace with count=1 limits to first occurrence (like Perl s///).
        escaped = new Regex(@"[A-Za-z0-9]+[A-Za-z0-9_\-]*").Replace(escaped, "($0)", count: 1);

        // Step 3: Perl wildcards — A = any char, X = optional any char
        // Replace escaped \A and \X (produced by Regex.Escape on uppercase letters — they're
        // not special in .NET regex so Regex.Escape leaves them as-is: just "A" and "X").
        escaped = escaped.Replace("A", ".").Replace("X", ".?");

        // Outer capture group wraps the entire tag (gives group 1 = full tag text)
        string outerPattern = $"({escaped})";

        bool tryStart = tag.Position is "START" or "EITHER";
        bool tryEnd   = tag.Position is "END"   or "EITHER";

        Match m = Match.Empty;

        if (tryStart)
            m = Regex.Match(name, $@"^{outerPattern}.+", RegexOptions.IgnoreCase);

        if (!m.Success && tryEnd)
            m = Regex.Match(name, $@".+{outerPattern}$", RegexOptions.IgnoreCase);

        if (!m.Success) return (null, null);

        // Group 1 = full tag text; group 2 = inner word (clan name), may be absent
        string matchedTag  = m.Groups[1].Value;
        string? clanName   = m.Groups.Count > 2 && m.Groups[2].Success
            ? m.Groups[2].Value
            : null;

        return (matchedTag, clanName);
    }
}
