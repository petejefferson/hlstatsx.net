using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles single-player objective/action events (flag captures, bomb plants, assists, etc.).
/// Mirrors <c>doEvent_PlayerAction</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class PlayerActionHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly TeamBonusHandler _teamBonus;
    private readonly ServerBroadcastService _broadcast;

    public PlayerActionHandler(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        TeamBonusHandler teamBonus,
        ServerBroadcastService broadcast)
    {
        _dbFactory  = dbFactory;
        _teamBonus  = teamBonus;
        _broadcast  = broadcast;
    }

    /// <param name="actionCode">The action code string from the log (e.g. "flag_capture").</param>
    public async Task HandleAsync(
        PlayerSession player,
        string actionCode,
        int? posX, int? posY, int? posZ,
        EventContext ctx,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (server.PlayerCount < server.Config.MinPlayers) return;
        if (server.IsInIgnoredBonusRound(ctx.EventUnix)) return;
        if (player.IsBot && server.Config.IgnoreBots) return;
        if (player.DbPlayerId <= 0) return;

        // Perl: if action == "headshot", mark next kill by this player as a headshot.
        if (actionCode.Equals("headshot", StringComparison.OrdinalIgnoreCase))
            server.NextKillHeadshot = player.DbPlayerId;

        // Perl: CTF flag carry-forward — track last defend time per team; reclassify drop
        // as "dropped_death" when the flag dropped within 1 second of opponent defending.
        if (actionCode.Equals("flagevent_defended", StringComparison.OrdinalIgnoreCase))
        {
            if (player.Team.Equals("Red", StringComparison.OrdinalIgnoreCase))
                server.LastRedFlagDefendUnix = ctx.EventUnix;
            else
                server.LastBlueFlagDefendUnix = ctx.EventUnix;
        }
        else if (actionCode.Equals("flagevent_dropped", StringComparison.OrdinalIgnoreCase))
        {
            if (player.Team.Equals("Red", StringComparison.OrdinalIgnoreCase) &&
                ctx.EventUnix - server.LastBlueFlagDefendUnix <= 1)
            {
                server.LastBlueFlagDefendUnix = 0;
                actionCode = "flagevent_dropped_death";
            }
            else if (player.Team.Equals("Blue", StringComparison.OrdinalIgnoreCase) &&
                     ctx.EventUnix - server.LastRedFlagDefendUnix <= 1)
            {
                server.LastRedFlagDefendUnix = 0;
                actionCode = "flagevent_dropped_death";
            }
        }

        await using var db = _dbFactory.CreateDbContext();

        // Look up the action definition (matches Perl's $g_games{game}{actions}{code})
        var action = await db.Set<GameAction>()
            .SingleOrDefaultAsync(a =>
                a.Game == server.Game &&
                a.Code == actionCode &&
                a.ForPlayerActions, ct);

        if (action is null) return;  // (IGNORED) — no matching action in DB

        // Log the event
        db.EventPlayerActions.Add(new EventPlayerAction
        {
            ServerId  = server.ServerId,
            PlayerId  = player.DbPlayerId,
            ActionId  = action.ActionId,
            Bonus     = action.RewardPlayer,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
            PosX      = posX,
            PosY      = posY,
            PosZ      = posZ,
        });

        // Increment the action's use counter
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Actions SET count = count + 1 WHERE game = {0} AND code = {1}",
            new object[] { server.Game, actionCode }, ct);

        // Apply skill reward to player
        player.Skill              += action.RewardPlayer;
        player.SessionSkillChange += action.RewardPlayer;

        await db.SaveChangesAsync(ct);

        // RCON broadcast: notify player of their action reward.
        // Mirrors Perl broadcasting_events + broadcasting_player_actions block in doEvent_PlayerAction.
        if (server.Config.BroadcastEvents && server.Config.BroadcastPlayerActions && action.RewardPlayer != 0)
        {
            string verb = action.RewardPlayer < 0 ? "lost" : "got";
            string msg  = $"{player.Name} {verb} {Math.Abs(action.RewardPlayer)} points ({player.Skill}) for {actionCode}";
            await _broadcast.MessagePlayerAsync(server, player, msg, ct);
        }

        // Team reward (e.g. all Allies score when a flag is capped)
        if (action.RewardTeam != 0 && actionCode != "pointcaptured")
        {
            var team = string.IsNullOrEmpty(action.Team) ? player.Team : action.Team;
            await _teamBonus.RewardTeamAsync(team, action.RewardTeam, action.ActionId, ctx, actionCode, ct);
        }
    }
}
