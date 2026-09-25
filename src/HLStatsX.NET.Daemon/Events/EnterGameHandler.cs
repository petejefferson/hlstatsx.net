using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles the player "entered the game" event, logging the entry to the DB.
/// </summary>
/// <remarks>
/// Mirrors <c>doEvent_EnterGame</c> in <c>HLstats_EventHandlers.plib</c>.
/// </remarks>
public sealed class EnterGameHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ServerBroadcastService _broadcast;
    private readonly ILogger<EnterGameHandler> _logger;

    public EnterGameHandler(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        ServerBroadcastService broadcast,
        ILogger<EnterGameHandler> logger)
    {
        _dbFactory = dbFactory;
        _broadcast = broadcast;
        _logger    = logger;
    }

    /// <summary>
    /// Called when the log line <c>"player" entered the game</c> is parsed.
    /// </summary>
    public async Task HandleAsync(
        PlayerSession player,
        EventContext ctx,
        CancellationToken ct = default)
    {
        // Bots: Perl updates connect_time if it was 0, then returns; we mirror that
        if (player.IsBot)
        {
            if (player.ConnectTime == 0)
                player.ConnectTime = ctx.EventUnix;
            _logger.LogDebug("(IGNORED) BOT enter game: {Name}", player.Name);
            return;
        }

        if (player.ConnectTime == 0)
            player.ConnectTime = ctx.EventUnix;

        if (player.DbPlayerId <= 0) return;  // pending/unresolved — no DB row yet

        await using var db = _dbFactory.CreateDbContext();
        db.EventEntries.Add(new EventEntry
        {
            ServerId  = ctx.Server.ServerId,
            PlayerId  = player.DbPlayerId,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
        });
        await db.SaveChangesAsync(ct);

        // Connect announce: broadcast "{name} (Pos {rank} with {kills} kills) has connected [{country}]"
        // Mirrors Perl doEvent_EnterGame connect_announce block in HLstats_EventHandlers.plib.
        if (ctx.Server.Config.ConnectAnnounce && player.DbPlayerId > 0)
        {
            var rank = await GetPlayerRankAsync(db, player.DbPlayerId, ctx.Server.Game, ct);
            var country = await GetPlayerCountryAsync(db, player.DbPlayerId, ct);
            string msg = string.IsNullOrEmpty(country)
                ? $"{player.Name} (Pos {rank} with {player.Kills} kills) has connected"
                : $"{player.Name} (Pos {rank} with {player.Kills} kills) has connected from {country}";
            await _broadcast.MessageAllAsync(ctx.Server, msg, force: true, ct: ct);
        }

        _logger.LogDebug("{Name} entered the game.", player.Name);
    }

    private static async Task<int> GetPlayerRankAsync(HLStatsDbContext db, int playerId, string game, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) + 1 AS Value FROM hlstats_Players
                WHERE game = {0} AND hideranking = 0
                AND skill > (SELECT skill FROM hlstats_Players WHERE playerId = {1})
                """,
                game, playerId)
            .ToListAsync(ct);
        return rows.Count > 0 ? rows[0] : 1;
    }

    private static async Task<string> GetPlayerCountryAsync(HLStatsDbContext db, int playerId, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<string>(
                "SELECT COALESCE(country, '') AS Value FROM hlstats_Players WHERE playerId = {0}",
                playerId)
            .ToListAsync(ct);
        return rows.Count > 0 ? rows[0] : string.Empty;
    }
}
