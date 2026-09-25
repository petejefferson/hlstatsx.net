using System.Data.Common;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoRibbons() from hlstats-awards.pl:
// - clears all player ribbons per game
// - re-awards ribbons based on award win count (special=0) or connection time (special=2)
public class RibbonsService : IRibbonsService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly ILogger<RibbonsService> _logger;

    public RibbonsService(IDbContextFactory<HLStatsDbContext> factory, ILogger<RibbonsService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<RibbonsResult> RecalculateAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            var games = await db.Games
                .Select(g => g.Code)
                .ToListAsync(ct);

            int ribbonsProcessed = 0;
            int playersAwarded = 0;

            foreach (var game in games)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM hlstats_Players_Ribbons WHERE game = {0}",
                    new object[] { game }, ct);

                var ribbons = await db.Ribbons
                    .Where(r => r.Game == game && (r.Special == 0 || r.Special == 2))
                    .Select(r => new { r.RibbonId, r.AwardCode, r.AwardCount, r.Special })
                    .ToListAsync(ct);

                foreach (var ribbon in ribbons)
                {
                    var playerIds = await GetQualifyingPlayerIdsAsync(conn, ribbon.RibbonId, ribbon.AwardCode, ribbon.AwardCount, ribbon.Special, game, ct);

                    foreach (var playerId in playerIds)
                    {
                        await db.Database.ExecuteSqlRawAsync(
                            "INSERT INTO hlstats_Players_Ribbons (playerId, ribbonId, game) VALUES ({0}, {1}, {2})",
                            new object[] { playerId, ribbon.RibbonId, game }, ct);
                        playersAwarded++;
                    }

                    ribbonsProcessed++;
                }
            }

            _logger.LogInformation(
                "Ribbons recalculated — {Games} games, {Ribbons} ribbons, {Players} player-ribbon records",
                games.Count, ribbonsProcessed, playersAwarded);

            return new RibbonsResult(games.Count, ribbonsProcessed, playersAwarded);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static async Task<List<int>> GetQualifyingPlayerIdsAsync(
        DbConnection conn, int ribbonId, string? awardCode, int awardCount, int special, string game, CancellationToken ct)
    {
        string sql;
        (string, object)[] parameters;

        if (special == 2)
        {
            // Connection-time ribbon: requires (connection_time / 3600) >= awardCount hours
            sql = @"
                SELECT playerId FROM hlstats_Players
                WHERE game = @game AND hideranking = 0
                  AND (connection_time / 3600) >= @count";
            parameters = [("@game", game), ("@count", (object)awardCount)];
        }
        else
        {
            // Award-win ribbon: requires winning the award >= awardCount times
            sql = @"
                SELECT pa.playerId
                FROM hlstats_Players_Awards pa
                INNER JOIN hlstats_Awards aw ON aw.awardId = pa.awardId AND aw.game = pa.game
                INNER JOIN hlstats_Players p ON p.playerId = pa.playerId AND p.hideranking = 0
                WHERE pa.game = @game AND aw.code = @code AND aw.awardType <> 'V'
                GROUP BY pa.playerId
                HAVING COUNT(pa.playerId) >= @count";
            parameters = [("@game", game), ("@code", (object)(awardCode ?? "")), ("@count", (object)awardCount)];
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        var ids = new List<int>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            ids.Add(reader.GetInt32(0));
        return ids;
    }
}
