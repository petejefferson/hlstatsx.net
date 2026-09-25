namespace HLStatsX.NET.Daemon.State;

/// <summary>
/// In-memory state for a player who is currently connected to a server.
/// Updated on every relevant event and flushed to the DB on disconnect.
/// Mirrors the <c>HLstats_Player</c> Perl object, excluding RCON fields.
/// </summary>
public sealed class PlayerSession
{
    // --- Identity ---
    public int DbPlayerId { get; set; }
    public int UserId { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string PlainUniqueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsBot { get; set; }
    public int? ClanId { get; set; }
    public int ServerId { get; set; }

    // --- Lifetime ---
    /// <summary>Unix timestamp when this session started (server clock).</summary>
    public long ConnectTime { get; set; }
    /// <summary>Unix timestamp of the most recent event from this player.</summary>
    public long LastEventTime { get; set; }
    /// <summary>
    /// Unix timestamp of the last team change (Perl: last_team_change).
    /// Used to ignore suicides within 2 seconds, and to suppress TK classification
    /// in TF2 when the victim switched teams just before the kill.
    /// </summary>
    public long LastTeamChangeUnix { get; set; }

    // --- Carried-over totals from DB (snapshot at session start) ---
    public int SkillAtConnect { get; set; }
    public int RankAtConnect { get; set; }

    // --- Session-only accumulators (reset each connect) ---
    public int SessionKills { get; set; }
    public int SessionDeaths { get; set; }
    public int SessionHeadshots { get; set; }
    public int SessionSuicides { get; set; }
    public int SessionTeamkills { get; set; }
    public int SessionShots { get; set; }
    public int SessionHits { get; set; }
    /// <summary>Net skill change during this session (can be negative). Used for livestats display.</summary>
    public int SessionSkillChange { get; set; }

    // --- Day-level skill tracking (mirrors Perl day_skill_change / last_update_skill) ---
    /// <summary>
    /// Cumulative skill change for today, loaded from hlstats_Players_History on connect
    /// and accumulated across every flush. Written as last_skill_change on each flush.
    /// </summary>
    public int DaySkillChange { get; set; }
    /// <summary>Player's skill at the end of the last flush. Zero until first flush (mirrors Perl last_update_skill).</summary>
    public int LastFlushSkill { get; set; }

    // --- Streak tracking ---
    /// <summary>Kills scored in the current life (resets on death/suicide).</summary>
    public int KillsThisLife { get; set; }
    /// <summary>Highest single-life kill streak achieved this session.</summary>
    public int SessionKillStreak { get; set; }
    /// <summary>Consecutive deaths in the current run (resets on a kill).</summary>
    public int DeathsInARow { get; set; }
    /// <summary>Highest consecutive-death run achieved this session (Perl: death_streak).</summary>
    public int SessionDeathStreak { get; set; }

    // --- DB values (current, updated live) ---
    public int Skill { get; set; } = 1000;
    public int Kills { get; set; }
    public int Deaths { get; set; }

    /// <summary>
    /// Whether the player has opted to receive in-game event messages.
    /// Defaults to the server's <c>DefaultDisplayEvents</c> setting.
    /// </summary>
    public bool DisplayEvents { get; set; } = true;
    public bool DisplayChat { get; set; } = true;

    public void UpdateTimestamp(long unixTime) => LastEventTime = unixTime;
}
