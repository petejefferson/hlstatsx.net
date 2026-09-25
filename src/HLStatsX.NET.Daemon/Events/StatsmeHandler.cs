using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles StatsMe weapon-accuracy events, recording per-weapon shot/hit/damage
/// data and accumulating shots/hits on the player session.
/// Mirrors <c>doEvent_Statsme</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class StatsmeHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public StatsmeHandler(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task HandleAsync(
        PlayerSession player,
        string weapon,
        int shots,
        int hits,
        int headshots,
        int damage,
        int kills,
        int deaths,
        EventContext ctx,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (server.PlayerCount < server.Config.MinPlayers) return;
        if (player.IsBot && server.Config.IgnoreBots) return;
        if (player.DbPlayerId <= 0) return;

        if (shots > 0)
        {
            player.SessionShots += shots;
        }
        if (hits > 0)
        {
            player.SessionHits += hits;
        }

        await using var db = _dbFactory.CreateDbContext();
        db.EventStatsme.Add(new EventStatsme
        {
            ServerId  = server.ServerId,
            PlayerId  = player.DbPlayerId,
            Weapon    = weapon,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
            Shots     = shots,
            Hits       = hits,
            Headshots  = headshots,
            Damage     = damage,
            Kills      = kills,
            Deaths     = deaths,
        });
        await db.SaveChangesAsync(ct);
    }
}
