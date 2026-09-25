using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles a player changing their in-game name. Flushes session stats for the old name,
/// updates the name alias in DB, and in NameTrack mode removes the session.
/// Mirrors <c>doEvent_ChangeName</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class ChangeNameHandler
{
    private readonly PlayerDbService _playerDb;
    private readonly ClanService _clanService;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public ChangeNameHandler(
        PlayerDbService playerDb,
        ClanService clanService,
        IDbContextFactory<HLStatsDbContext> dbFactory)
    {
        _playerDb    = playerDb;
        _clanService = clanService;
        _dbFactory   = dbFactory;
    }

    public async Task HandleAsync(
        PlayerSession player,
        string newName,
        EventContext ctx,
        DaemonOptions options,
        CancellationToken ct = default)
    {
        if (player.IsBot && ctx.Server.Config.IgnoreBots)
        {
            ctx.Server.RemovePlayer(player.UserId, player.UniqueId);
            return;
        }

        if (player.DbPlayerId > 0)
        {
            // Flush stats accumulated under the old name (leaveLastUse=true in Perl — don't update lastuse)
            await _playerDb.FlushSessionAsync(player, ctx.EventUnix, ctx.IsReplay, ct);

            // Ensure the new alias exists in hlstats_PlayerNames
            await using var db = _dbFactory.CreateDbContext();
            await EnsurePlayerNameAsync(db, player.DbPlayerId, newName, ct);
        }

        // NameTrack mode: treat name change like a disconnect + lose session
        if (options.Mode.Equals("NameTrack", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Server.RemovePlayer(player.UserId, player.UniqueId);
            return;
        }

        player.Name   = newName;
        player.ClanId = await _clanService.MatchAsync(newName, player.Game, ct);
        player.UpdateTimestamp(ctx.EventUnix);
    }

    private static async Task EnsurePlayerNameAsync(
        HLStatsDbContext db,
        int playerId,
        string name,
        CancellationToken ct)
    {
        var exists = await db.Set<Core.Entities.PlayerName>()
            .AnyAsync(n => n.PlayerId == playerId && n.Name == name, ct);

        if (!exists)
        {
            db.Set<Core.Entities.PlayerName>().Add(new Core.Entities.PlayerName
            {
                PlayerId = playerId,
                Name     = name,
                LastUse  = DateTime.Now,
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
