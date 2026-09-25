using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.Geo;
using HLStatsX.NET.Daemon.Parsing;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles the player connect event — creates the in-memory <see cref="PlayerSession"/>
/// and loads or creates the player DB record.
/// </summary>
/// <remarks>
/// Mirrors <c>doEvent_Connect</c> in <c>HLstats_EventHandlers.plib</c>.
/// </remarks>
public sealed class ConnectHandler
{
    private readonly PlayerDbService _playerDb;
    private readonly GeoIpLookup _geoIp;
    private readonly LivestatService _liveStat;
    private readonly ServerBroadcastService _broadcast;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<ConnectHandler> _logger;

    public ConnectHandler(
        PlayerDbService playerDb,
        GeoIpLookup geoIp,
        LivestatService liveStat,
        ServerBroadcastService broadcast,
        IDbContextFactory<HLStatsDbContext> dbFactory,
        ILogger<ConnectHandler> logger)
    {
        _playerDb  = playerDb;
        _geoIp     = geoIp;
        _liveStat  = liveStat;
        _broadcast = broadcast;
        _dbFactory = dbFactory;
        _logger    = logger;
    }

    /// <summary>
    /// Called when the log line <c>"player" connected, address "ip:port"</c> is parsed.
    /// </summary>
    /// <param name="playerInfo">Parsed player string from the log line.</param>
    /// <param name="ipAddress">Player's IP address (without port).</param>
    /// <param name="ctx">Current event context.</param>
    public async Task HandleAsync(
        PlayerInfo playerInfo,
        string ipAddress,
        EventContext ctx,
        DaemonOptions options,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        // Bots with IgnoreBots=true skip DB load but still get a session so other
        // events don't have to special-case a null player.
        if (playerInfo.IsBot && server.Config.IgnoreBots)
        {
            var botSession = BuildSession(playerInfo, ipAddress, ctx, options);
            server.AddPlayer(botSession);
            _logger.LogDebug("(IGNORED) BOT connect: {Name}", playerInfo.Name);
            return;
        }

        // Pending IDs (STEAM_ID_PENDING, LAN, etc.) cannot be resolved yet —
        // the Perl daemon stores a pre-connect entry and processes on EnterGame.
        if (PlayerStringParser.IsPending(playerInfo.UniqueId))
        {
            _logger.LogDebug("Pending uniqueid {UniqueId} for {Name} — deferring.", playerInfo.UniqueId, playerInfo.Name);
            return;
        }

        var session = BuildSession(playerInfo, ipAddress, ctx, options);

        // Kick any existing session with the same uniqueid (reconnect without disconnect)
        var existing = server.FindByUniqueId(playerInfo.UniqueId);
        if (existing is not null && existing.UserId != playerInfo.UserId)
        {
            _logger.LogDebug("Reconnect detected for {UniqueId} — flushing old session.", playerInfo.UniqueId);
            await _playerDb.FlushSessionAsync(existing, ctx.EventUnix, ctx.IsReplay, ct);
            server.RemovePlayer(existing.UserId, existing.UniqueId);
        }

        // Load or create DB record
        await _playerDb.EnsurePlayerAsync(
            session,
            playerInfo.UniqueId,
            server.Game,
            server.Config.DefaultDisplayEvents,
            ct);

        // Update lastAddress on connect (Perl: getAddress → UPDATE hlstats_Players SET lastAddress)
        if (session.DbPlayerId > 0 && !string.IsNullOrEmpty(ipAddress))
        {
            await using var db = _dbFactory.CreateDbContext();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE hlstats_Players SET lastAddress = {0} WHERE playerId = {1}",
                new object[] { ipAddress, session.DbPlayerId }, ct);
        }

        // GeoIP lookup — only when enabled and player has a routable IP
        if (options.UseGeoIpBinary && session.DbPlayerId > 0)
            await _geoIp.LookupAndUpdateAsync(session.DbPlayerId, ipAddress, options.GeoIpDatabasePath, ct);

        // Insert live-stats row (UDP mode only — mirrors Perl $g_stdin == 0 check)
        await _liveStat.UpsertAsync(session, ct);

        server.AddPlayer(session);

        // Rank kick: if MinRank > 0, kick players whose rank number exceeds the threshold.
        // Mirrors Perl doEvent_Connect min_players_rank check in HLstats_EventHandlers.plib.
        if (server.Config.MinRank > 0 && session.DbPlayerId > 0)
        {
            int rank = await GetPlayerRankAsync(session.DbPlayerId, server.Game, ct);
            if (rank > server.Config.MinRank || rank == 0)
            {
                _logger.LogDebug("Kicking {Name} (rank={Rank}) — server requires top {MinRank}.",
                    session.Name, rank, server.Config.MinRank);
                await _broadcast.KickPlayerAsync(server, session, $"Not a Top {server.Config.MinRank}-Player", ct);
            }
        }

        _logger.LogDebug("{Name} connected from {Ip} (dbId={DbId}).",
            session.Name, ipAddress, session.DbPlayerId);
    }

    private async Task<int> GetPlayerRankAsync(int playerId, string game, CancellationToken ct)
    {
        await using var db = _dbFactory.CreateDbContext();
        // Rank = position in the skill-ordered list of visible players for this game.
        // Mirrors Perl HLstats_Player->getRank().
        var rank = await db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) + 1 AS Value FROM hlstats_Players
                WHERE game = {0} AND hideranking = 0
                AND skill > (SELECT skill FROM hlstats_Players WHERE playerId = {1})
                """,
                game, playerId)
            .FirstOrDefaultAsync(ct);
        return rank;
    }

    private static PlayerSession BuildSession(
        PlayerInfo playerInfo,
        string ipAddress,
        EventContext ctx,
        DaemonOptions options)
        => new()
        {
            UserId        = playerInfo.UserId,
            UniqueId      = playerInfo.UniqueId,
            PlainUniqueId = playerInfo.PlainUniqueId,
            Name          = playerInfo.Name,
            Game          = ctx.Server.Game,
            Team          = playerInfo.Team,
            Role          = playerInfo.Role,
            IpAddress     = ipAddress,
            IsBot         = playerInfo.IsBot,
            ConnectTime   = ctx.EventUnix,
            LastEventTime = ctx.EventUnix,
            DisplayEvents = ctx.Server.Config.DefaultDisplayEvents,
            ServerId      = ctx.Server.ServerId,
        };
}
