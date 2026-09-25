using HLStatsX.NET.Daemon.Query;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles map change events ("loading" and "started").
/// Mirrors <c>doEvent_ChangeMap</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class MapChangeHandler
{
    private readonly PlayerDbService _playerDb;
    private readonly PlayerActionHandler _playerAction;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly A2SQueryService _a2sQuery;
    private readonly ILogger<MapChangeHandler> _logger;

    public MapChangeHandler(
        PlayerDbService playerDb,
        PlayerActionHandler playerAction,
        IDbContextFactory<HLStatsDbContext> dbFactory,
        A2SQueryService a2sQuery,
        ILogger<MapChangeHandler> logger)
    {
        _playerDb     = playerDb;
        _playerAction = playerAction;
        _dbFactory    = dbFactory;
        _a2sQuery     = a2sQuery;
        _logger       = logger;
    }

    /// <param name="type">Either <c>"loading"</c> or <c>"started"</c>.</param>
    /// <param name="newMap">The incoming map name.</param>
    public async Task HandleAsync(
        string type,
        string newMap,
        ServerState server,
        long nowUnix,
        bool isReplay = false,
        CancellationToken ct = default)
    {
        // Perl sets $self->{map} = $newmap unconditionally at the top of doEvent_ChangeMap,
        // before the loading/started branch. Mirror that so events between Loading and Started
        // (disconnects, connects, etc.) record the correct map name.
        server.CurrentMap = newMap;

        if (type.Equals("loading", StringComparison.OrdinalIgnoreCase))
        {
            // Remove bots (they don't carry over between maps)
            var bots = server.Players.Values.Where(p => p.IsBot).ToList();
            foreach (var bot in bots)
            {
                await _playerDb.FlushSessionAsync(bot, nowUnix, isReplay, ct);
                server.RemovePlayer(bot.UserId, bot.UniqueId);
            }

            // End kill streaks for remaining players (Perl: endKillStreak per player at map loading)
            var mapCtx = new EventContext
            {
                Server    = server,
                EventUnix = nowUnix,
                Map       = server.CurrentMap,
                IsReplay  = isReplay,
            };
            foreach (var player in server.Players.Values)
            {
                if (player.KillsThisLife >= 2)
                    await _playerAction.HandleAsync(player, $"kill_streak_{Math.Min(player.KillsThisLife, 12)}", null, null, null, mapCtx, ct);
                player.KillsThisLife = 0;
            }

            _logger.LogInformation("Loading map \"{Map}\" on server {Server}.", newMap, server.Address);
        }
        else if (type.Equals("started", StringComparison.OrdinalIgnoreCase))
        {
            server.MapStartedUnix      = nowUnix;
            server.InBonusRound        = false;
            server.BonusRoundStartUnix = 0;

            // A2S_INFO fallback: confirm map name from live server when address is a routable IP.
            // Mirrors Perl's queryServer call in HLstats_Server.pm get_map() STATUSFAIL branch.
            if (!isReplay &&
                !string.IsNullOrEmpty(server.Address) &&
                !server.Address.Equals("127.0.0.1", StringComparison.Ordinal) &&
                !server.Address.StartsWith("::1", StringComparison.Ordinal))
            {
                var info = await _a2sQuery.QueryAsync(server.Address, server.Port, ct);
                if (info is not null && !string.IsNullOrEmpty(info.MapName) && info.MapName != newMap)
                {
                    _logger.LogDebug("A2S_INFO map correction: log said \"{LogMap}\", server reports \"{QueryMap}\".",
                        newMap, info.MapName);
                    server.CurrentMap = info.MapName;
                }
            }

            // Persist act_map and map_started to hlstats_Servers (mirrors Perl updateDB on map start)
            await using var db = _dbFactory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE hlstats_Servers SET
                  act_map      = {0},
                  map_started  = {1},
                  map_changes  = map_changes + 1
                WHERE serverId = {2}
                """,
                new object[] { server.CurrentMap, (int)nowUnix, server.ServerId },
                ct);

            // Reset per-player map-level accumulators (Perl: map_kills, map_deaths, team = "")
            foreach (var player in server.Players.Values)
                player.Team = "";

            _logger.LogInformation("Map started: \"{Map}\" on server {Server}.", server.CurrentMap, server.Address);
        }
    }
}
