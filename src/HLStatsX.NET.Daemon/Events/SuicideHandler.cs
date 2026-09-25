using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles suicide events — applies the suicide skill penalty and logs to DB.
/// Mirrors <c>doEvent_Suicide</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class SuicideHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public SuicideHandler(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task HandleAsync(
        PlayerSession player,
        string weapon,
        int? posX, int? posY, int? posZ,
        EventContext ctx,
        DaemonOptions options,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (server.PlayerCount < server.Config.MinPlayers)
        {
            return;  // (IGNORED) NOTMINPLAYERS
        }
        if (server.IsInIgnoredBonusRound(ctx.EventUnix))
        {
            return;  // (IGNORED) BonusRound
        }
        if (player.IsBot && server.Config.IgnoreBots)
        {
            return;  // (IGNORED) BOT
        }

        // Perl: ignore suicide within 2 seconds of a team change (prevents phantom suicides on switch)
        if (player.LastTeamChangeUnix + 2 > ctx.EventUnix)
        {
            return;  // (IGNORED) TEAMSWITCH
        }

        int penalty = server.Config.SuicidePenalty;

        // Kill-streak ends on suicide; death streak grows
        player.KillsThisLife = 0;
        player.DeathsInARow++;
        if (player.DeathsInARow > player.SessionDeathStreak)
            player.SessionDeathStreak = player.DeathsInARow;

        player.Skill              -= penalty;
        player.SessionSkillChange -= penalty;
        player.SessionSuicides++;

        await using var db = _dbFactory.CreateDbContext();
        db.EventSuicides.Add(new EventSuicide
        {
            ServerId  = server.ServerId,
            PlayerId  = player.DbPlayerId,
            WeaponCode = weapon,
            Map       = ctx.Map,
            EventTime = ctx.EventTime,
            PosX      = posX,
            PosY      = posY,
            PosZ      = posZ,
        });
        await db.SaveChangesAsync(ct);
    }
}
