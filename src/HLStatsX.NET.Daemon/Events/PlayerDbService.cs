using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles all database operations related to player identity and session flushing,
/// mirroring the Perl daemon's <c>HLstats_Player</c> object and <c>getPlayerId</c> /
/// <c>insertPlayer</c> / <c>flushDB</c> subroutines.
/// </summary>
public sealed class PlayerDbService
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ClanService _clanService;
    private readonly ILogger<PlayerDbService> _logger;

    private readonly LivestatService _liveStat;

    public PlayerDbService(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        ClanService clanService,
        LivestatService liveStat,
        ILogger<PlayerDbService> logger)
    {
        _dbFactory   = dbFactory;
        _clanService = clanService;
        _liveStat    = liveStat;
        _logger      = logger;
    }

    /// <summary>
    /// Looks up the <see cref="Player"/> record for <paramref name="uniqueId"/> in
    /// <paramref name="game"/>, creating one if it does not yet exist.
    /// On success, populates <paramref name="session"/> with the player's DB identity
    /// and current skill/kills/deaths.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the player was found or created; <see langword="false"/>
    /// when the unique ID is a pending/LAN sentinel that cannot be resolved yet.
    /// </returns>
    public async Task<bool> EnsurePlayerAsync(
        PlayerSession session,
        string uniqueId,
        string game,
        bool defaultDisplayEvents,
        CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Lookup by unique ID + game (mirrors Perl getPlayerId)
        var uid = await db.Set<PlayerUniqueId>()
            .SingleOrDefaultAsync(u => u.UniqueId == uniqueId && u.Game == game, ct);

        int playerId;

        if (uid is not null)
        {
            playerId = uid.PlayerId;
            var player = await db.Set<Player>()
                .SingleOrDefaultAsync(p => p.PlayerId == playerId, ct);

            if (player is null)
            {
                // Orphaned UID row — create a Player row to match
                player = await InsertPlayerAsync(db, session.Name, game, defaultDisplayEvents, ct);
                playerId = player.PlayerId;
            }

            session.DbPlayerId        = playerId;
            session.Skill             = player.Skill;
            session.SkillAtConnect    = player.Skill;
            session.Kills             = player.Kills;
            session.Deaths            = player.Deaths;
            session.DisplayEvents     = player.HideRanking == 0 && defaultDisplayEvents;
        }
        else
        {
            // New player: insert Player row then PlayerUniqueId row
            var player = await InsertPlayerAsync(db, session.Name, game, defaultDisplayEvents, ct);
            playerId = player.PlayerId;

            db.Set<PlayerUniqueId>().Add(new PlayerUniqueId
            {
                PlayerId = playerId,
                UniqueId = uniqueId,
                Game     = game,
            });
            await db.SaveChangesAsync(ct);

            session.DbPlayerId        = playerId;
            session.Skill             = 1000;
            session.SkillAtConnect    = 1000;
            session.DisplayEvents     = defaultDisplayEvents;

            _logger.LogDebug("Created new player record {PlayerId} for uniqueId={UniqueId}.",
                playerId, uniqueId);
        }

        // Ensure a PlayerName record exists for the current alias
        await EnsurePlayerNameAsync(db, playerId, session.Name, ct);

        // Load today's skill_change from history and initialise LastFlushSkill.
        // Mirrors Perl check_history: day_skill_change is loaded from hlstats_Players_History
        // for today's date so that multiple sessions in the same day accumulate correctly.
        var connectDate = DateTimeOffset.FromUnixTimeSeconds(session.ConnectTime).LocalDateTime.Date;
        var nextDay     = connectDate.AddDays(1);
        session.DaySkillChange = await db.Set<PlayerHistory>()
            .Where(h => h.PlayerId == playerId && h.Game == game
                     && h.EventTime >= connectDate && h.EventTime < nextDay)
            .Select(h => (int?)h.SkillChange)
            .FirstOrDefaultAsync(ct) ?? 0;
        // Mirrors Perl: last_update_skill is set to current skill at end of the connect-time flushDB.
        session.LastFlushSkill = session.Skill;

        // Clan tag matching (mirrors Perl getClanId called from setName on connect)
        session.ClanId = await _clanService.MatchAsync(session.Name, game, ct);

        return true;
    }

    /// <summary>
    /// Atomically flushes accumulated session stats to <c>hlstats_Players</c> and
    /// <c>hlstats_PlayerNames</c>, matching the Perl daemon's <c>flushDB</c> behaviour.
    /// </summary>
    /// <remarks>
    /// Uses raw SQL with parameterised queries for atomic INCREMENT operations that
    /// EF Core change-tracking cannot express without first loading the row.
    /// </remarks>
    public async Task FlushSessionAsync(PlayerSession session, long nowUnix, bool isReplay = false, CancellationToken ct = default)
    {
        if (session.DbPlayerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();

        var sessionSeconds = (int)(nowUnix - session.ConnectTime);
        if (sessionSeconds < 0) sessionSeconds = 0;
        // Perl stdin mode: gaps > 600 s are treated as idle and contribute nothing to connection_time.
        if (isReplay && sessionSeconds > 600) sessionSeconds = 0;

        // Accumulate day skill change (mirrors Perl day_skill_change / last_update_skill).
        // LastFlushSkill == 0 means no prior flush this session yet — skip the delta.
        if (session.LastFlushSkill > 0)
            session.DaySkillChange += session.Skill - session.LastFlushSkill;

        // Perl: if ($skill < 0) {$skill = 0;} — HLstats_Player.pm:579
        if (session.Skill < 0) session.Skill = 0;

        _logger.LogDebug(
            "FlushSession pid={PlayerId} name={Name} replay={IsReplay} " +
            "sessionSecs={SessionSecs} +kills={Kills} +deaths={Deaths} +suicides={Suicides} " +
            "+teamkills={Teamkills} +headshots={Headshots} " +
            "skill={Skill} daySkillChange={DaySkillChange} " +
            "deathStreak={DeathStreak} killStreak={KillStreak}",
            session.DbPlayerId, session.Name, isReplay,
            sessionSeconds, session.SessionKills, session.SessionDeaths, session.SessionSuicides,
            session.SessionTeamkills, session.SessionHeadshots,
            session.Skill, session.DaySkillChange,
            session.SessionDeathStreak, session.SessionKillStreak);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE hlstats_Players SET
              connection_time   = connection_time + {0},
              lastName          = {1},
              kills             = kills + {2},
              deaths            = deaths + {3},
              suicides          = suicides + {4},
              skill             = {5},
              headshots         = headshots + {6},
              shots             = shots + {7},
              hits              = hits + {8},
              teamkills         = teamkills + {9},
              last_event        = {10},
              last_skill_change = {11},
              death_streak      = IF({12} > death_streak, {12}, death_streak),
              kill_streak       = IF({13} > kill_streak, {13}, kill_streak),
              hideranking       = IF(hideranking = 3, 0, hideranking),
              activity          = 100,
              clan              = {14}
            WHERE playerId = {15}
            """,
            new object[] { sessionSeconds, session.Name, session.SessionKills,
                           session.SessionDeaths, session.SessionSuicides, session.Skill,
                           session.SessionHeadshots, session.SessionShots, session.SessionHits,
                           session.SessionTeamkills, (int)nowUnix, session.DaySkillChange,
                           session.SessionDeathStreak, session.SessionKillStreak,
                           session.ClanId ?? 0, session.DbPlayerId },
            ct);

        // Update alias stats
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE hlstats_PlayerNames SET
              connection_time = connection_time + {0},
              kills           = kills + {1},
              deaths          = deaths + {2},
              suicides        = suicides + {3},
              headshots       = headshots + {4},
              shots           = shots + {5},
              hits            = hits + {6},
              lastuse         = FROM_UNIXTIME({7})
            WHERE playerId = {8} AND name = {9}
            """,
            new object[] { sessionSeconds, session.SessionKills, session.SessionDeaths,
                           session.SessionSuicides, session.SessionHeadshots, session.SessionShots,
                           session.SessionHits, (int)nowUnix, session.DbPlayerId, session.Name },
            ct);

        // Update live-stats row with current session values
        await _liveStat.UpdateAsync(session, ct);

        // Reset session accumulators (Perl: flushDB resets kills/deaths/etc to 0 after each flush)
        session.SessionKills       = 0;
        session.SessionDeaths      = 0;
        session.SessionSuicides    = 0;
        session.SessionHeadshots   = 0;
        session.SessionShots       = 0;
        session.SessionHits        = 0;
        session.SessionTeamkills   = 0;
        session.SessionSkillChange = 0;
        session.SessionKillStreak  = 0;
        session.SessionDeathStreak = 0;
        session.KillsThisLife      = 0;
        session.DeathsInARow       = 0;
        session.ConnectTime        = nowUnix;
        session.LastFlushSkill     = session.Skill;
    }

    // --- Private helpers ---

    private static async Task<Player> InsertPlayerAsync(
        HLStatsDbContext db,
        string name,
        string game,
        bool displayEvents,
        CancellationToken ct)
    {
        var player = new Player
        {
            LastName      = name,
            Game          = game,
            ClanId        = 0,
            Skill         = 1000,
            CreateDate    = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            HideRanking   = 0,
        };
        db.Set<Player>().Add(player);
        await db.SaveChangesAsync(ct);
        return player;
    }

    private static async Task EnsurePlayerNameAsync(
        HLStatsDbContext db,
        int playerId,
        string name,
        CancellationToken ct)
    {
        var existing = await db.Set<PlayerName>()
            .SingleOrDefaultAsync(n => n.PlayerId == playerId && n.Name == name, ct);

        if (existing is null)
        {
            db.Set<PlayerName>().Add(new PlayerName
            {
                PlayerId = playerId,
                Name     = name,
                LastUse  = DateTime.Now,
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
