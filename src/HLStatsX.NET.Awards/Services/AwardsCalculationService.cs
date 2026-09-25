using System.Data.Common;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoAwards() from hlstats-awards.pl:
// - finds daily and all-time winners for each award definition
// - updates hlstats_Awards with winner ids/counts
// - inserts winning records into hlstats_Players_Awards
public class AwardsCalculationService : IAwardsCalculationService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<AwardsCalculationService> _logger;

    public AwardsCalculationService(
        IDbContextFactory<HLStatsDbContext> factory,
        IConfiguration config,
        ILogger<AwardsCalculationService> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public async Task<AwardsCalculationResult> CalculateAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var numDays = _config.GetValue<int>("HLStatsX:Awards:NumDays", 1);
        var dateBase = DateTime.Today;
        var awardsDate = dateBase.AddDays(-1);
        var awardsDateStr = awardsDate.ToString("yyyy-MM-dd");

        await UpsertAwardsDateAsync(db, awardsDateStr, ct);

        await db.Database.ExecuteSqlRawAsync(
            "REPLACE INTO hlstats_Options (keyname, value, opttype) VALUES ('awards_numdays', {0}, 2)",
            new object[] { numDays }, ct);

        var awards = await db.Awards
            .Join(db.Games, a => a.Game, g => g.Code, (a, g) => new { Award = a, Game = g })
            .Where(x => x.Game.Hidden != "1")
            .Select(x => x.Award)
            .OrderBy(a => a.Game).ThenBy(a => a.AwardType)
            .ToListAsync(ct);

        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            int processed = 0;
            foreach (var award in awards)
            {
                var (daily, global) = await GetWinnersAsync(conn, award, dateBase, numDays, ct);

                await db.Database.ExecuteSqlRawAsync(
                    @"UPDATE hlstats_Awards
                      SET d_winner_id = {0}, d_winner_count = {1},
                          g_winner_id = {2}, g_winner_count = {3}
                      WHERE awardId = {4}",
                    new object[] { Param(daily?.PlayerId), Param(daily?.AwardCount), Param(global?.PlayerId), Param(global?.AwardCount), award.AwardId },
                    ct);

                processed++;
            }

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT IGNORE INTO hlstats_Players_Awards
                  SELECT value, awardId, d_winner_id, d_winner_count, game
                  FROM hlstats_Options INNER JOIN hlstats_Awards
                  WHERE keyname = 'awards_d_date' AND d_winner_id IS NOT NULL",
                ct);

            _logger.LogInformation("Awards processed: {Count}, date: {Date}", processed, awardsDateStr);
            return new AwardsCalculationResult(processed, awardsDateStr);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task<AwardsPreviewResult> PreviewAsync(DateOnly? date = null, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var numDays = _config.GetValue<int>("HLStatsX:Awards:NumDays", 1);
        var awardsDate = date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var dateBase = awardsDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var awardsDateStr = awardsDate.ToString("yyyy-MM-dd");

        var awards = await db.Awards
            .Join(db.Games, a => a.Game, g => g.Code, (a, g) => new { Award = a, Game = g })
            .Where(x => x.Game.Hidden != "1")
            .Select(x => x.Award)
            .OrderBy(a => a.Game).ThenBy(a => a.AwardType)
            .ToListAsync(ct);

        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            var rows = new List<AwardsPreviewRow>();
            foreach (var award in awards)
            {
                var (daily, _) = await GetWinnersAsync(conn, award, dateBase, numDays, ct);

                string? playerName = null;
                if (daily?.PlayerId is int pid)
                    playerName = await db.Players
                        .Where(p => p.PlayerId == pid)
                        .Select(p => p.LastName)
                        .FirstOrDefaultAsync(ct);

                rows.Add(new AwardsPreviewRow(
                    award.Game,
                    award.Code,
                    award.Name,
                    award.AwardType,
                    daily?.PlayerId,
                    playerName,
                    daily?.AwardCount));
            }

            return new AwardsPreviewResult(awardsDateStr, rows);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static async Task UpsertAwardsDateAsync(HLStatsDbContext db, string awardsDate, CancellationToken ct)
    {
        var existing = await db.Options.Where(o => o.KeyName == "awards_d_date").SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE hlstats_Options SET value = {0} WHERE keyname = 'awards_d_date'",
                new object[] { awardsDate }, ct);
        }
        else
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO hlstats_Options (keyname, value, opttype) VALUES ('awards_d_date', {0}, 2)",
                new object[] { awardsDate }, ct);
        }
    }

    private static async Task<(WinnerRow? Daily, WinnerRow? Global)> GetWinnersAsync(
        DbConnection conn, Award award, DateTime dateBase, int numDays, CancellationToken ct)
    {
        var (dailySql, dailyParams, globalSql, globalParams) = BuildQueries(award, dateBase, numDays);

        // latency: sort ASC (lowest wins), all others DESC — already handled in per-query ORDER BY
        var daily = await ReadWinnerAsync(conn, dailySql, dailyParams, ct);
        var global = await ReadWinnerAsync(conn, globalSql, globalParams, ct);

        if (daily?.AwardCount < 1) daily = null;
        if (global?.AwardCount < 1) global = null;

        return (daily, global);
    }

    private static async Task<WinnerRow?> ReadWinnerAsync(
        DbConnection conn, string sql, IReadOnlyList<(string Name, object Value)> parameters, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        if (reader.IsDBNull(0)) return null;
        return new WinnerRow(reader.GetInt32(0), reader.GetInt32(1));
    }

    // Returns (dailySql, dailyParams, globalSql, globalParams)
    internal static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildQueries(Award award, DateTime dateBase, int numDays)
    {
        var cutoff = dateBase.AddDays(-numDays);

        return award.Code switch
        {
            "latency" => BuildLatencyQueries(award.Game, dateBase, cutoff),
            "mostkills" => BuildHistoryPlayerQueries("kills", award.Game, dateBase, cutoff),
            "suicide" => BuildHistoryPlayerQueries("suicides", award.Game, dateBase, cutoff),
            "teamkills" => BuildHistoryPlayerQueries("teamkills", award.Game, dateBase, cutoff),
            "bonuspoints" => BuildBonusPointsQueries(award.Game, dateBase, cutoff),
            "allsentrykills" => BuildSentryKillsQueries(award.Game, dateBase, cutoff),
            "connectiontime" => BuildHistoryPlayerQueries("connection_time", award.Game, dateBase, cutoff),
            "killstreak" => BuildHistoryPlayerQueries("kill_streak", award.Game, dateBase, cutoff),
            "deathstreak" => BuildHistoryPlayerQueries("death_streak", award.Game, dateBase, cutoff),
            _ => BuildGenericQueries(award, dateBase, cutoff),
        };
    }

    private static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildLatencyQueries(string game, DateTime dateBase, DateTime cutoff)
    {
        const string daily = @"
            SELECT e.playerId, ROUND(ROUND(SUM(e.ping) / COUNT(e.ping), 0) / 2, 0) AS cnt
            FROM hlstats_Events_Latency e
            INNER JOIN hlstats_Servers s ON s.serverId = e.serverId AND s.game = @game
            INNER JOIN hlstats_Players p ON p.playerId = e.playerId AND p.hideranking = 0
            WHERE e.eventTime < @date AND e.eventTime > @cutoff
            GROUP BY e.playerId ORDER BY cnt, MIN(e.eventTime) ASC, e.playerId ASC LIMIT 1";

        const string global = @"
            SELECT e.playerId, ROUND(ROUND(SUM(e.ping) / COUNT(e.ping), 0) / 2, 0) AS cnt
            FROM hlstats_Events_Latency e
            INNER JOIN hlstats_Servers s ON s.serverId = e.serverId AND s.game = @game
            INNER JOIN hlstats_Players p ON p.playerId = e.playerId AND p.hideranking = 0
            GROUP BY e.playerId ORDER BY cnt, MIN(e.eventTime) ASC, e.playerId ASC LIMIT 1";

        var dp = Params(("@game", game), ("@date", dateBase), ("@cutoff", cutoff));
        var gp = Params(("@game", game));
        return (daily, dp, global, gp);
    }

    private static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildHistoryPlayerQueries(string field, string game, DateTime dateBase, DateTime cutoff)
    {
        var daily = $@"
            SELECT h.playerId, h.{field} AS cnt
            FROM hlstats_Players_History h
            INNER JOIN hlstats_Players p ON p.playerId = h.playerId AND p.hideranking = 0
            WHERE h.game = @game AND h.eventTime = @cutoff
            ORDER BY cnt DESC, h.playerId ASC LIMIT 1";

        var global = $@"
            SELECT p.playerId, p.{field} AS cnt
            FROM hlstats_Players p
            WHERE p.game = @game AND p.hideranking = 0
            ORDER BY cnt DESC, p.playerId ASC LIMIT 1";

        var dp = Params(("@game", game), ("@cutoff", cutoff));
        var gp = Params(("@game", game));
        return (daily, dp, global, gp);
    }

    private static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildBonusPointsQueries(string game, DateTime dateBase, DateTime cutoff)
    {
        const string daily = @"
            SELECT actions.playerId, SUM(actions.bonus) AS cnt
            FROM (
                SELECT playerId, bonus, serverId, eventTime FROM hlstats_Events_PlayerActions
                WHERE eventTime < @date AND eventTime > @cutoff
                UNION ALL
                SELECT playerId, bonus, serverId, eventTime FROM hlstats_Events_PlayerPlayerActions
                WHERE eventTime < @date AND eventTime > @cutoff
            ) actions
            INNER JOIN hlstats_Servers s ON s.serverId = actions.serverId AND s.game = @game
            INNER JOIN hlstats_Players p ON p.playerId = actions.playerId AND p.hideranking = 0
            GROUP BY actions.playerId ORDER BY cnt DESC, MIN(actions.eventTime) ASC, actions.playerId ASC LIMIT 1";

        const string global = @"
            SELECT actions.playerId, SUM(actions.bonus) AS cnt
            FROM (
                SELECT playerId, bonus, serverId, eventTime FROM hlstats_Events_PlayerActions
                UNION ALL
                SELECT playerId, bonus, serverId, eventTime FROM hlstats_Events_PlayerPlayerActions
            ) actions
            INNER JOIN hlstats_Servers s ON s.serverId = actions.serverId AND s.game = @game
            INNER JOIN hlstats_Players p ON p.playerId = actions.playerId AND p.hideranking = 0
            GROUP BY actions.playerId ORDER BY cnt DESC, MIN(actions.eventTime) ASC, actions.playerId ASC LIMIT 1";

        var dp = Params(("@game", game), ("@date", dateBase), ("@cutoff", cutoff));
        var gp = Params(("@game", game));
        return (daily, dp, global, gp);
    }

    private static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildSentryKillsQueries(string game, DateTime dateBase, DateTime cutoff)
    {
        const string daily = @"
            SELECT f.killerId, COUNT(f.weapon) AS cnt
            FROM hlstats_Events_Frags f
            INNER JOIN hlstats_Players p ON p.playerId = f.killerId AND p.hideranking = 0
            WHERE f.eventTime < @date AND f.eventTime > @cutoff
              AND p.game = @game AND f.weapon LIKE 'obj_sentrygun%'
            GROUP BY f.killerId ORDER BY cnt DESC, p.skill DESC, MIN(f.eventTime) ASC, f.killerId ASC LIMIT 1";

        const string global = @"
            SELECT f.killerId, COUNT(f.weapon) AS cnt
            FROM hlstats_Events_Frags f
            INNER JOIN hlstats_Players p ON p.playerId = f.killerId AND p.hideranking = 0
            WHERE p.game = @game AND f.weapon LIKE 'obj_sentrygun%'
            GROUP BY f.killerId ORDER BY cnt DESC, p.skill DESC, MIN(f.eventTime) ASC, f.killerId ASC LIMIT 1";

        var dp = Params(("@game", game), ("@date", dateBase), ("@cutoff", cutoff));
        var gp = Params(("@game", game));
        return (daily, dp, global, gp);
    }

    private static (string, IReadOnlyList<(string, object)>, string, IReadOnlyList<(string, object)>)
        BuildGenericQueries(Award award, DateTime dateBase, DateTime cutoff)
    {
        // Table/field selection matches the Perl awardType switch block
        string table, join, matchField, playerField;
        string effectiveCode = award.Code;

        switch (award.AwardType)
        {
            case "O":
                table = "hlstats_Events_PlayerActions";
                join = "LEFT JOIN hlstats_Actions a ON a.id = t.actionId";
                matchField = "a.code";
                playerField = "t.playerId";
                break;
            case "W":
                table = "hlstats_Events_Frags";
                join = "";
                if (award.Code == "headshot")
                {
                    matchField = "t.headshot";
                    effectiveCode = "1";
                }
                else
                {
                    matchField = "t.weapon";
                }
                playerField = "t.killerId";
                break;
            case "P":
                table = "hlstats_Events_PlayerPlayerActions";
                join = "LEFT JOIN hlstats_Actions a ON a.id = t.actionId";
                matchField = "a.code";
                playerField = "t.playerId";
                break;
            case "V":
                table = "hlstats_Events_PlayerPlayerActions";
                join = "LEFT JOIN hlstats_Actions a ON a.id = t.actionId";
                matchField = "a.code";
                playerField = "t.victimId";
                break;
            default:
                return (EmptySql, [], EmptySql, []);
        }

        var daily = $@"
            SELECT {playerField}, COUNT({matchField}) AS cnt
            FROM {table} t
            INNER JOIN hlstats_Players p ON p.playerId = {playerField} AND p.hideranking = 0
            {join}
            WHERE t.eventTime < @date AND t.eventTime > @cutoff
              AND p.game = @game AND {matchField} = @code
            GROUP BY {playerField} ORDER BY cnt DESC, p.skill DESC, MIN(t.eventTime) ASC, {playerField} ASC LIMIT 1";

        var global = $@"
            SELECT {playerField}, COUNT({matchField}) AS cnt
            FROM {table} t
            INNER JOIN hlstats_Players p ON p.playerId = {playerField} AND p.hideranking = 0
            {join}
            WHERE p.game = @game AND {matchField} = @code
            GROUP BY {playerField} ORDER BY cnt DESC, p.skill DESC, MIN(t.eventTime) ASC, {playerField} ASC LIMIT 1";

        var dp = Params(("@game", award.Game), ("@date", (object)dateBase), ("@cutoff", cutoff), ("@code", effectiveCode));
        var gp = Params(("@game", award.Game), ("@code", (object)effectiveCode));
        return (daily, dp, global, gp);
    }

    private const string EmptySql = "SELECT NULL, 0 WHERE 1=0";

    private static IReadOnlyList<(string Name, object Value)> Params(
        params (string Name, object Value)[] items) => items;

    private static object Param(int? value) => value.HasValue ? value.Value : DBNull.Value;

    private sealed class WinnerRow
    {
        public WinnerRow(int playerId, int awardCount) { PlayerId = playerId; AwardCount = awardCount; }
        public int PlayerId { get; }
        public int AwardCount { get; }
    }
}
