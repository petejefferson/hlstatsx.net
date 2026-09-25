using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Rewards all players on a given team for a group action (e.g. round win, flag capture).
/// Mirrors the Perl daemon's <c>rewardTeam</c> subroutine and <c>recordEvent("TeamBonuses", ...)</c>.
/// </summary>
/// <remarks>
/// Only players who have sent an event in the last 2 minutes are eligible (matching Perl's
/// idle-player exclusion).
/// </remarks>
public sealed class TeamBonusHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ServerBroadcastService _broadcast;

    public TeamBonusHandler(IDbContextFactory<HLStatsDbContext> dbFactory, ServerBroadcastService broadcast)
    {
        _dbFactory = dbFactory;
        _broadcast = broadcast;
    }

    /// <param name="team">Team name to reward (e.g. "Allies", "Axis").</param>
    /// <param name="reward">Skill points to add (positive) or deduct (negative).</param>
    /// <param name="actionId">DB ID of the triggering action (for event logging).</param>
    /// <param name="actionCode">Action code string used in the broadcast message.</param>
    public async Task RewardTeamAsync(
        string team,
        int reward,
        int actionId,
        EventContext ctx,
        string actionCode = "",
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        // Perl: only reward players active within the last 2 minutes
        long cutoff = ctx.EventUnix - 120;

        var eligible = server.Players.Values
            .Where(p =>
                p.Team.Equals(team, StringComparison.OrdinalIgnoreCase) &&
                p.LastEventTime >= cutoff &&
                !(p.IsBot && server.Config.IgnoreBots) &&
                p.DbPlayerId > 0)
            .ToList();

        if (eligible.Count == 0) return;

        await using var db = _dbFactory.CreateDbContext();

        foreach (var player in eligible)
        {
            db.Set<EventPlayerAction>().Add(new EventPlayerAction
            {
                ServerId  = server.ServerId,
                PlayerId  = player.DbPlayerId,
                ActionId  = actionId,
                Bonus     = reward,
                Map       = ctx.Map,
                EventTime = ctx.EventTime,
            });

            player.Skill              += reward;
            player.SessionSkillChange += reward;
        }

        await db.SaveChangesAsync(ct);

        // RCON broadcast: message each eligible display_events player with the team reward.
        // Mirrors Perl rewardTeam messageMany call in hlstats.pl.
        if (server.Config.BroadcastEvents && server.Config.BroadcastPlayerActions)
        {
            string verb = reward < 0 ? "lost" : "got";
            string msg  = $"Your team {verb} {Math.Abs(reward)} points for {actionCode}";
            foreach (var player in eligible)
                await _broadcast.MessagePlayerAsync(server, player, msg, ct);
        }
    }
}
