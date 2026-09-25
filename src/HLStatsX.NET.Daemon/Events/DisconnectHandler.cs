using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles player disconnect: flushes session stats to DB and removes the player
/// from the server's in-memory registry.
/// </summary>
/// <remarks>
/// Mirrors <c>doEvent_Disconnect</c> in <c>HLstats_EventHandlers.plib</c>.
/// VAC/SteamBans ban handling and RCON auto-ban are deferred.
/// </remarks>
public sealed class DisconnectHandler
{
    private readonly PlayerDbService _playerDb;
    private readonly PlayerActionHandler _playerAction;
    private readonly LivestatService _liveStat;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<DisconnectHandler> _logger;

    public DisconnectHandler(
        PlayerDbService playerDb,
        PlayerActionHandler playerAction,
        LivestatService liveStat,
        IDbContextFactory<HLStatsDbContext> dbFactory,
        ILogger<DisconnectHandler> logger)
    {
        _playerDb     = playerDb;
        _playerAction = playerAction;
        _liveStat     = liveStat;
        _dbFactory    = dbFactory;
        _logger       = logger;
    }

    /// <summary>
    /// Called when the log line <c>"player" disconnected</c> is parsed.
    /// </summary>
    public async Task HandleAsync(
        PlayerSession player,
        EventContext ctx,
        string disconnectReason,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (player.IsBot && server.Config.IgnoreBots)
        {
            // Bots: Perl calls updateDB (no-op) then removes — mirror that
            server.RemovePlayer(player.UserId, player.UniqueId);
            _logger.LogDebug("(IGNORED) BOT disconnect: {Name}", player.Name);
            return;
        }

        if (player.DbPlayerId > 0)
        {
            // Perl: endKillStreak fires before updateDB on disconnect (line 623)
            if (player.KillsThisLife >= 2)
                await _playerAction.HandleAsync(player, $"kill_streak_{Math.Min(player.KillsThisLife, 12)}", null, null, null, ctx, ct);

            // Log the disconnect event
            await using var db = _dbFactory.CreateDbContext();
            db.EventDisconnects.Add(new EventDisconnect
            {
                ServerId  = server.ServerId,
                PlayerId  = player.DbPlayerId,
                Map       = ctx.Map,
                EventTime = ctx.EventTime,
            });
            await db.SaveChangesAsync(ct);

            // Flush accumulated session stats
            await _playerDb.FlushSessionAsync(player, ctx.EventUnix, ctx.IsReplay, ct);

            // Remove live-stats row
            await _liveStat.DeleteAsync(player.DbPlayerId, ct);
        }

        server.RemovePlayer(player.UserId, player.UniqueId);

        _logger.LogDebug("{Name} disconnected ({Reason}).", player.Name, disconnectReason);
    }
}
