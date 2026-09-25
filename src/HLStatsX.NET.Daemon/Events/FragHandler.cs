using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.Skill;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles kill events — including regular frags, teamkills, and the associated
/// skill calculation, kill-streak tracking, and DB updates.
/// </summary>
/// <remarks>
/// Mirrors <c>doEvent_Frag</c> in <c>HLstats_EventHandlers.plib</c>. Position data from
/// kill log lines is recorded when present.
/// </remarks>
public sealed class FragHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly PlayerActionHandler _playerAction;
    private readonly ServerBroadcastService _broadcast;
    private readonly ILogger<FragHandler> _logger;

    public FragHandler(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        PlayerActionHandler playerAction,
        ServerBroadcastService broadcast,
        ILogger<FragHandler> logger)
    {
        _dbFactory     = dbFactory;
        _playerAction  = playerAction;
        _broadcast     = broadcast;
        _logger        = logger;
    }

    /// <param name="killer">In-memory session for the killing player.</param>
    /// <param name="victim">In-memory session for the killed player.</param>
    /// <param name="weapon">Weapon code (normalised to lowercase).</param>
    /// <param name="headshot">Whether the kill was a headshot.</param>
    /// <param name="posKillerX/Y/Z">Killer 3-D position (null when not reported by game).</param>
    /// <param name="posVictimX/Y/Z">Victim 3-D position (null when not reported by game).</param>
    /// <param name="weaponModifier">Weapon skill modifier loaded from <c>hlstats_Weapons</c>.</param>
    /// <param name="ctx">Current event context.</param>
    /// <param name="options">Global daemon options.</param>
    public async Task HandleAsync(
        PlayerSession killer,
        PlayerSession victim,
        string weapon,
        bool headshot,
        int? posKillerX, int? posKillerY, int? posKillerZ,
        int? posVictimX, int? posVictimY, int? posVictimZ,
        double weaponModifier,
        EventContext ctx,
        DaemonOptions options,
        CancellationToken ct = default)
    {
        var server = ctx.Server;

        if (killer.IsBot && server.Config.IgnoreBots ||
            victim.IsBot && server.Config.IgnoreBots)
        {
            _logger.LogDebug("(IGNORED) BOT frag: {Killer} → {Victim}", killer.Name, victim.Name);
            return;
        }

        bool isTeamKill = SameTeam(killer.Team, victim.Team) &&
                          server.Config.GameType != 1;  // GameType=1 = free-for-all

        // Perl (TF2 only): if the victim switched teams within the last 2 seconds, don't count as TK.
        // TF2 reports death after the team switch, so the teams appear to match even though they didn't.
        if (isTeamKill &&
            server.Game.Equals("tf", StringComparison.OrdinalIgnoreCase) &&
            victim.LastTeamChangeUnix + 2 > ctx.EventUnix)
        {
            isTeamKill = false;
        }

        await using var db = _dbFactory.CreateDbContext();

        // Perl: dod_bomb_target teamkills are explicitly ignored ("IGNORED BOMBED TEAMKILL DODS").
        // Bomb target splash damage killing teammates is a normal DoD:S game mechanic, not a penalty.
        if (isTeamKill && weapon.Equals("dod_bomb_target", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("(IGNORED) dod_bomb_target teamkill: {Killer} → {Victim}", killer.Name, victim.Name);
            return;
        }

        if (!isTeamKill)
            await HandleFragAsync(killer, victim, weapon, headshot,
                posKillerX, posKillerY, posKillerZ,
                posVictimX, posVictimY, posVictimZ,
                weaponModifier, ctx, options, db, _broadcast, ct);
        else
            await HandleTeamKillAsync(killer, victim, weapon, headshot,
                posKillerX, posKillerY, posKillerZ,
                posVictimX, posVictimY, posVictimZ,
                ctx, options, db, _broadcast, ct);

        await db.SaveChangesAsync(ct);
    }

    // --- Private: regular frag ---

    private async Task HandleFragAsync(
        PlayerSession killer,
        PlayerSession victim,
        string weapon,
        bool headshot,
        int? pkX, int? pkY, int? pkZ,
        int? pvX, int? pvY, int? pvZ,
        double weaponModifier,
        EventContext ctx,
        DaemonOptions options,
        HLStatsDbContext db,
        ServerBroadcastService broadcast,
        CancellationToken ct)
    {
        var server = ctx.Server;

        // Save victim's streak before EndKillStreak resets it (Perl: kills_per_life)
        int victimStreak = victim.KillsThisLife;

        // Kill-streak tracking (Perl: kills_per_life += 1, reset on victim)
        killer.KillsThisLife++;
        if (killer.KillsThisLife > killer.SessionKillStreak)
            killer.SessionKillStreak = killer.KillsThisLife;
        killer.DeathsInARow = 0;  // a kill resets the killer's consecutive-death run

        EndKillStreak(victim);  // victim dies → their streak resets

        // Fire kill_streak_N action for victim if they had a streak (Perl: endKillStreak → doEvent_PlayerAction).
        // Fired before calcSkill so victim.Skill includes the streak bonus when ELO runs.
        if (victimStreak >= 2)
            await _playerAction.HandleAsync(victim, $"kill_streak_{Math.Min(victimStreak, 12)}", null, null, null, ctx, ct);

        // Log the frag event
        db.EventFrags.Add(new EventFrag
        {
            ServerId    = server.ServerId,
            KillerId    = killer.DbPlayerId,
            VictimId    = victim.DbPlayerId,
            Weapon      = weapon,
            Headshot    = headshot,
            KillerRole  = killer.Role,
            VictimRole  = victim.Role,
            Map         = ctx.Map,
            EventTime   = ctx.EventTime,
            PosX        = pkX,
            PosY        = pkY,
            PosZ        = pkZ,
            PosVictimX  = pvX,
            PosVictimY  = pvY,
            PosVictimZ  = pvZ,
        });

        // Skill calculation
        var skillResult = SkillCalculator.Calc(
            server.Config.SkillMode,
            killer.Skill, killer.Kills,
            victim.Skill, victim.Kills,
            weaponModifier,
            options.SkillMaxChange,
            options.SkillMinChange,
            options.PlayerMinKills,
            options.SkillRatioCap,
            killer.Team);

        int killerGain = skillResult.KillerSkill - killer.Skill;
        int victimLoss = victim.Skill - skillResult.VictimSkill;

        // Update in-memory session accumulators
        killer.Skill             = skillResult.KillerSkill;
        killer.SessionSkillChange += killerGain;
        killer.SessionKills++;
        killer.Kills++;
        if (headshot)
        {
            killer.SessionHeadshots++;
        }

        victim.Skill              = skillResult.VictimSkill;
        victim.SessionSkillChange -= victimLoss;
        victim.SessionDeaths++;
        victim.Deaths++;
        victim.KillsThisLife = 0;  // victim's life ends
        victim.DeathsInARow++;
        if (victim.DeathsInARow > victim.SessionDeathStreak)
            victim.SessionDeathStreak = victim.DeathsInARow;

        // Atomic DB increments for weapon and map counts
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Weapons SET kills = kills + 1" +
            (headshot ? ", headshots = headshots + 1" : "") +
            " WHERE game = {0} AND code = {1}",
            new object[] { ctx.Server.Game, weapon }, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO hlstats_Maps_Counts (game, map, kills, headshots)
            VALUES ({0}, {1}, 1, {2})
            ON DUPLICATE KEY UPDATE kills = kills + 1
            """,
            new object[] { ctx.Server.Game, ctx.Map, headshot ? 1 : 0 }, ct);

        _logger.LogDebug("{Killer} killed {Victim} with {Weapon}{Hs} (+{Gain}/-{Loss}).",
            killer.Name, victim.Name, weapon,
            headshot ? " [HS]" : "",
            killerGain, victimLoss);

        // RCON broadcast: message killer and victim with kill result.
        // Mirrors Perl broadcasting_events block in HLstats_EventHandlers.plib doEvent_Frag.
        if (server.Config.BroadcastEvents)
        {
            string msg = $"{killer.Name} ({killer.Skill}) got {killerGain} points for killing {victim.Name} ({victim.Skill})";
            await broadcast.MessagePlayerAsync(server, killer, msg, ct);
            await broadcast.MessagePlayerAsync(server, victim, msg, ct);
        }
    }

    // --- Private: teamkill ---

    private async Task HandleTeamKillAsync(
        PlayerSession killer,
        PlayerSession victim,
        string weapon,
        bool headshot,
        int? pkX, int? pkY, int? pkZ,
        int? pvX, int? pvY, int? pvZ,
        EventContext ctx,
        DaemonOptions options,
        HLStatsDbContext db,
        ServerBroadcastService broadcast,
        CancellationToken ct)
    {
        var server = ctx.Server;
        int penalty = ctx.Server.Config.TkPenalty;

        db.EventTeamkills.Add(new EventTeamkill
        {
            ServerId   = server.ServerId,
            KillerId   = killer.DbPlayerId,
            VictimId   = victim.DbPlayerId,
            WeaponCode = weapon,
            Map        = ctx.Map,
            EventTime  = ctx.EventTime,
            PosX       = pkX,
            PosY       = pkY,
            PosZ       = pkZ,
            PosVictimX = pvX,
            PosVictimY = pvY,
            PosVictimZ = pvZ,
        });

        killer.Skill             -= penalty;
        killer.SessionSkillChange -= penalty;
        killer.SessionTeamkills++;

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Weapons SET kills = kills + 1" +
            (headshot ? ", headshots = headshots + 1" : "") +
            " WHERE game = {0} AND code = {1}",
            new object[] { ctx.Server.Game, weapon }, ct);

        _logger.LogDebug("TEAMKILL: {Killer} killed {Victim} with {Weapon} (-{Penalty} pts).",
            killer.Name, victim.Name, weapon, penalty);

        // RCON broadcast for teamkill. Mirrors Perl broadcasting_events block.
        if (ctx.Server.Config.BroadcastEvents)
        {
            string msg = $"{killer.Name} lost {penalty} points ({killer.Skill}) for team-killing";
            await broadcast.MessagePlayerAsync(ctx.Server, killer, msg, ct);
        }
    }

    // --- Helpers ---

    private static void EndKillStreak(PlayerSession player)
    {
        // Perl endKillStreak: streak count is already tracked via kills_per_life.
        // In the .NET model we track KillsThisLife; reset it on death.
        player.KillsThisLife = 0;
    }

    private static bool SameTeam(string teamA, string teamB)
    {
        if (string.IsNullOrEmpty(teamA) || string.IsNullOrEmpty(teamB)) return false;
        return string.Equals(teamA, teamB, StringComparison.OrdinalIgnoreCase);
    }
}
