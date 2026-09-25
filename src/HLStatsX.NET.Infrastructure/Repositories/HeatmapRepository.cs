using System.Data;
using System.Data.Common;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class HeatmapRepository : IHeatmapRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public HeatmapRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<HeatmapConfig?> GetConfigAsync(string game, string map, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.HeatmapConfigs.SingleOrDefaultAsync(c => c.Game == game && c.Map == map, ct);
    }

    public async Task<IReadOnlyList<HeatPoint>> GetKillPointsAsync(
        string game, string map, int days, int binSize, int? playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await QueryPointsAsync(db, "pos_x", "pos_y", "killerId", game, map, days, binSize, playerId, ct);
    }

    public async Task<IReadOnlyList<HeatPoint>> GetDeathPointsAsync(
        string game, string map, int days, int binSize, int? playerId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await QueryPointsAsync(db, "pos_victim_x", "pos_victim_y", "victimId", game, map, days, binSize, playerId, ct);
    }

    private static async Task<List<HeatPoint>> QueryPointsAsync(
        HLStatsDbContext db,
        string xCol, string yCol, string playerIdCol,
        string game, string map, int days, int binSize, int? playerId,
        CancellationToken ct)
    {
        var sql = $"""
            SELECT CAST(ROUND(ef.{xCol} / @binSize) * @binSize AS SIGNED) AS bin_x,
                   CAST(ROUND(ef.{yCol} / @binSize) * @binSize AS SIGNED) AS bin_y,
                   COUNT(*) AS cnt
            FROM hlstats_Events_Frags ef
            JOIN hlstats_Servers s ON s.serverId = ef.serverId
            WHERE s.game = @game
              AND ef.map = @map
              AND ef.{xCol} IS NOT NULL AND ef.{xCol} != 0
              AND ef.{yCol} IS NOT NULL AND ef.{yCol} != 0
            """;

        if (days > 0)
            sql += "\n  AND ef.eventTime >= @cutoff";
        if (playerId.HasValue)
            sql += $"\n  AND ef.{playerIdCol} = @playerId";

        sql += "\nGROUP BY bin_x, bin_y";

        await db.Database.OpenConnectionAsync(ct);
        var conn = db.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParam(cmd, "@binSize", binSize);
        AddParam(cmd, "@game", game);
        AddParam(cmd, "@map", map);
        if (days > 0)
            AddParam(cmd, "@cutoff", DateTime.Now.AddDays(-days));
        if (playerId.HasValue)
            AddParam(cmd, "@playerId", playerId.Value);

        var result = new List<HeatPoint>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var x = Convert.ToInt64(reader.GetValue(0));
            var y = Convert.ToInt64(reader.GetValue(1));
            var value = Convert.ToInt32(reader.GetValue(2));
            result.Add(new HeatPoint(x, y, value));
        }
        return result;
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
