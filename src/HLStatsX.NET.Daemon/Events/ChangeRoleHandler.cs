using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles a player switching roles/classes. Updates session state, logs the change,
/// and increments the role's pick counter in <c>hlstats_Roles</c>.
/// Mirrors <c>doEvent_RoleSelection</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class ChangeRoleHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public ChangeRoleHandler(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task HandleAsync(
        PlayerSession player,
        string newRole,
        EventContext ctx,
        CancellationToken ct = default)
    {
        player.Role = newRole;
        player.UpdateTimestamp(ctx.EventUnix);

        if (player.IsBot && ctx.Server.Config.IgnoreBots) return;
        if (player.DbPlayerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();

        db.EventChangeRoles.Add(new EventChangeRole
        {
            ServerId  = ctx.Server.ServerId,
            PlayerId  = player.DbPlayerId,
            Role      = newRole,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
        });

        // Increment the role pick counter (matches Perl's execNonQuery UPDATE hlstats_Roles)
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Roles SET picked = picked + 1 WHERE game = {0} AND code = {1}",
            new object[] { ctx.Server.Game, newRole }, ct);

        await db.SaveChangesAsync(ct);
    }
}
