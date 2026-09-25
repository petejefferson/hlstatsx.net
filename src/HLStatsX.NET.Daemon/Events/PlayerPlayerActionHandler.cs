using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles two-player action events (assists, dominations, headshots against a named target, etc.).
/// The acting player gains skill; the victim loses the same amount.
/// Mirrors <c>doEvent_PlayerPlayerAction</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class PlayerPlayerActionHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly TeamBonusHandler _teamBonus;
    private readonly ServerBroadcastService _broadcast;

    public PlayerPlayerActionHandler(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        TeamBonusHandler teamBonus,
        ServerBroadcastService broadcast)
    {
        _dbFactory  = dbFactory;
        _teamBonus  = teamBonus;
        _broadcast  = broadcast;
    }

    public async Task HandleAsync(
        PlayerSession player,
        PlayerSession victim,
        string actionCode,
        int? posX, int? posY, int? posZ,
        int? posVictimX, int? posVictimY, int? posVictimZ,
        EventContext ctx,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (player.UserId == victim.UserId) return;  // (IGNORED) PLAYER SAME AS VICTIM
        if (server.PlayerCount < server.Config.MinPlayers) return;
        if (server.IsInIgnoredBonusRound(ctx.EventUnix)) return;

        if ((player.IsBot || victim.IsBot) && server.Config.IgnoreBots) return;
        if (player.DbPlayerId <= 0 || victim.DbPlayerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();

        var action = await db.Set<GameAction>()
            .SingleOrDefaultAsync(a =>
                a.Game == server.Game &&
                a.Code == actionCode &&
                a.ForPlayerPlayerActions, ct);

        if (action is null) return;  // (IGNORED)

        db.EventPlayerPlayerActions.Add(new EventPlayerPlayerAction
        {
            ServerId   = server.ServerId,
            PlayerId   = player.DbPlayerId,
            VictimId   = victim.DbPlayerId,
            ActionId   = action.ActionId,
            Bonus      = action.RewardPlayer,
            Map        = ctx.Map,
            EventTime  = ctx.EventTime,
            PosX       = posX,
            PosY       = posY,
            PosZ       = posZ,
            PosVictimX = posVictimX,
            PosVictimY = posVictimY,
            PosVictimZ = posVictimZ,
        });

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Actions SET count = count + 1 WHERE game = {0} AND code = {1}",
            new object[] { server.Game, actionCode }, ct);

        if (action.RewardPlayer != 0)
        {
            player.Skill              += action.RewardPlayer;
            player.SessionSkillChange += action.RewardPlayer;
            victim.Skill              -= action.RewardPlayer;
            victim.SessionSkillChange -= action.RewardPlayer;
        }

        await db.SaveChangesAsync(ct);

        // RCON broadcast: notify acting player of their pvp action reward.
        // Mirrors Perl broadcasting_events + broadcasting_player_actions block in doEvent_PlayerPlayerAction.
        if (server.Config.BroadcastEvents && server.Config.BroadcastPlayerActions && action.RewardPlayer != 0)
        {
            string verb = action.RewardPlayer < 0 ? "lost" : "got";
            string msg  = $"{player.Name} {verb} {Math.Abs(action.RewardPlayer)} points ({player.Skill}) for {actionCode}";
            await _broadcast.MessagePlayerAsync(server, player, msg, ct);
        }

        if (!string.IsNullOrEmpty(action.Team) && action.RewardTeam != 0)
        {
            await _teamBonus.RewardTeamAsync(action.Team, action.RewardTeam, action.ActionId, ctx, actionCode, ct);
        }
    }
}
