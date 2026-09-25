using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoClans() from hlstats-awards.pl:
// - reads clan tag patterns, builds regexes
// - matches every player's name against the patterns
// - creates new clan records when a tag is seen for the first time
// - updates each player's clan field (0 = no clan)
public class ClansService : IClansService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly ILogger<ClansService> _logger;

    public ClansService(IDbContextFactory<HLStatsDbContext> factory, ILogger<ClansService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ClansResult> RecalculateAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Load clan tags sorted by length DESC, then id — matches Perl ORDER BY pattern_length DESC, id
        var tags = await db.ClanTags
            .OrderByDescending(t => t.Pattern.Length)
            .ThenBy(t => t.Id)
            .Select(t => new { t.Pattern, t.Position })
            .ToListAsync(ct);

        var matcher = new ClanPatternMatcher(tags.Select(t => (t.Pattern, t.Position)));

        var players = await db.Players
            .Select(p => new { p.PlayerId, p.LastName, p.Game })
            .ToListAsync(ct);

        // Cache existing clans: (tag, game) → clanId
        var existingClans = await db.Clans
            .Select(c => new { c.Tag, c.Game, c.ClanId })
            .ToDictionaryAsync(c => (c.Tag, c.Game), c => c.ClanId, ct);

        int clansCreated = 0;
        int playersUpdated = 0;
        int playersCleared = 0;

        foreach (var player in players)
        {
            ct.ThrowIfCancellationRequested();

            var match = matcher.Match(player.LastName);

            if (match is null)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE hlstats_Players SET clan = 0 WHERE playerId = {0}",
                    new object[] { player.PlayerId }, ct);
                playersCleared++;
                continue;
            }

            var key = (match.Tag, player.Game);
            if (!existingClans.TryGetValue(key, out var clanId))
            {
                // Create new clan
                await db.Database.ExecuteSqlRawAsync(
                    "REPLACE INTO hlstats_Clans (tag, name, game) VALUES ({0}, {1}, {2})",
                    new object[] { match.Tag, match.Name, player.Game }, ct);

                // Read back the generated clanId
                clanId = await db.Clans
                    .Where(c => c.Tag == match.Tag && c.Game == player.Game)
                    .Select(c => c.ClanId)
                    .SingleOrDefaultAsync(ct);

                existingClans[key] = clanId;
                clansCreated++;
            }

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE hlstats_Players SET clan = {0} WHERE playerId = {1}",
                new object[] { clanId, player.PlayerId }, ct);
            playersUpdated++;
        }

        _logger.LogInformation(
            "Clans recalculated — {Total} players, {Created} new clans, {Updated} assigned, {Cleared} cleared",
            players.Count, clansCreated, playersUpdated, playersCleared);

        return new ClansResult(players.Count, clansCreated, playersUpdated, playersCleared);
    }

}
