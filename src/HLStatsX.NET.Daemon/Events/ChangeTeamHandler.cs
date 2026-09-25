using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles a player switching teams. Updates the in-memory session and logs to DB.
/// Mirrors <c>doEvent_TeamSelection</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class ChangeTeamHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public ChangeTeamHandler(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task HandleAsync(
        PlayerSession player,
        string newTeam,
        EventContext ctx,
        CancellationToken ct = default)
    {
        player.Team = newTeam;
        player.UpdateTimestamp(ctx.EventUnix);

        if (player.IsBot && ctx.Server.Config.IgnoreBots) return;

        // Perl: $player->set("last_team_change", $ev_remotetime) for non-bot players.
        // Consumed by SuicideHandler (2-second grace) and FragHandler (TF2 TK suppression).
        player.LastTeamChangeUnix = ctx.EventUnix;
        if (player.DbPlayerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();
        db.EventChangeTeams.Add(new EventChangeTeam
        {
            ServerId  = ctx.Server.ServerId,
            PlayerId  = player.DbPlayerId,
            Team      = newTeam,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
        });
        await db.SaveChangesAsync(ct);
    }
}
