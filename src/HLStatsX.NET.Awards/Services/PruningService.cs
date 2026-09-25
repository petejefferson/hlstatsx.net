using System.Data;
using System.Data.Common;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Services;

public class PruningService : IPruningService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<PruningService> _logger;

    // All event tables from the original Perl %g_eventTables hash in HLstats.plib
    private static readonly string[] EventTables =
    [
        "TeamBonuses", "ChangeRole", "ChangeName", "ChangeTeam",
        "Connects", "Disconnects", "Entries", "Frags",
        "PlayerActions", "PlayerPlayerActions", "Suicides", "Teamkills",
        "Rcon", "Admin", "Statsme", "Statsme2",
        "StatsmeLatency", "StatsmeTime", "Latency", "Chat"
    ];

    public PruningService(
        IDbContextFactory<HLStatsDbContext> factory,
        IConfiguration config,
        ILogger<PruningService> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public async Task<PruningResult> PruneAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var deleteDays = await GetDeleteDaysAsync(db, ct);
        // Batch size 0 = no batching (single DELETE per table, original behaviour).
        // Default 10 000 keeps each transaction small enough for InnoDB to handle comfortably.
        var batchSize = _config.GetValue<int>("HLStatsX:Awards:Pruning:BatchSize", 10_000);

        _logger.LogInformation(
            "Pruning data older than {DeleteDays} days{Batching}",
            deleteDays,
            batchSize > 0 ? $" (batch size: {batchSize:N0})" : " (no batching)");

        // Open the connection once and reuse it across all count + delete queries.
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            var eventRows = await PruneEventTablesAsync(conn, deleteDays, batchSize, ct);
            _logger.LogInformation("Event tables total: {Count:N0} rows deleted", eventRows);

            var historyRows = await PruneTableAsync(conn,
                "hlstats_Players_History",
                "eventTime < DATE_SUB(CURRENT_TIMESTAMP(), INTERVAL ? DAY)",
                [deleteDays], batchSize, ct);

            // Trends older than 2 days are always discarded regardless of DeleteDays
            var trendRows = await PruneTableAsync(conn,
                "hlstats_Trend",
                "`timestamp` < (UNIX_TIMESTAMP() - 172800)",
                [], batchSize, ct);

            // Server load history is always kept for exactly one year
            var serverLoadRows = await PruneTableAsync(conn,
                "hlstats_server_load",
                "`timestamp` < UNIX_TIMESTAMP(DATE_SUB(NOW(), INTERVAL 1 YEAR))",
                [], batchSize, ct);

            return new PruningResult(deleteDays, eventRows, historyRows, trendRows, serverLoadRows);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private async Task<long> PruneEventTablesAsync(
        DbConnection conn, int deleteDays, int batchSize, CancellationToken ct)
    {
        long total = 0;
        foreach (var table in EventTables)
        {
            var deleted = await PruneTableAsync(conn,
                $"hlstats_Events_{table}",
                "eventTime < DATE_SUB(CURRENT_TIMESTAMP(), INTERVAL ? DAY)",
                [deleteDays], batchSize, ct);
            total += deleted;
        }
        return total;
    }

    private async Task<long> PruneTableAsync(
        DbConnection conn,
        string tableName,
        string whereClause,
        object[] parameters,
        int batchSize,
        CancellationToken ct)
    {
        var pending = await CountAsync(conn,
            $"SELECT COUNT(*) FROM `{tableName}` WHERE {whereClause}",
            parameters, ct);

        if (pending == 0)
        {
            _logger.LogDebug("{Table}: 0 rows eligible — skipping", tableName);
            return 0;
        }

        _logger.LogInformation("{Table}: {Pending:N0} rows eligible for deletion", tableName, pending);

        var deleteSql = $"DELETE FROM `{tableName}` WHERE {whereClause}";
        if (batchSize > 0)
            deleteSql += $" LIMIT {batchSize}";

        long total = 0;
        int batches = 0;
        int deleted;
        do
        {
            deleted = await ExecuteNonQueryAsync(conn, deleteSql, parameters, ct);
            total += deleted;
            batches++;
            if (batchSize > 0 && deleted >= batchSize)
                _logger.LogInformation("{Table}: batch {Batch} — {Deleted:N0} deleted, ~{Remaining:N0} remaining",
                    tableName, batches, total, pending - total);
        }
        while (batchSize > 0 && deleted >= batchSize);

        if (batches > 1)
            _logger.LogInformation("{Table}: deleted {Count:N0} rows in {Batches} batches", tableName, total, batches);
        else
            _logger.LogInformation("{Table}: deleted {Count:N0} rows", tableName, total);

        return total;
    }

    private static async Task<long> CountAsync(
        DbConnection conn, string sql, object[] parameters, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParams(cmd, parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    private static async Task<int> ExecuteNonQueryAsync(
        DbConnection conn, string sql, object[] parameters, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParams(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddParams(IDbCommand cmd, object[] parameters)
    {
        foreach (var value in parameters)
        {
            var p = cmd.CreateParameter();
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }

    private static async Task<int> GetDeleteDaysAsync(HLStatsDbContext db, CancellationToken ct)
    {
        var value = await db.Options
            .Where(o => o.KeyName == "DeleteDays")
            .Select(o => o.Value)
            .SingleOrDefaultAsync(ct);

        return int.TryParse(value, out var days) && days > 0 ? days : 30;
    }
}
