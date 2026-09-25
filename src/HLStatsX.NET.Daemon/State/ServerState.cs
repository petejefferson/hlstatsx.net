using HLStatsX.NET.Daemon.Configuration;

namespace HLStatsX.NET.Daemon.State;

/// <summary>
/// In-memory state for an active game server, including its live player registry
/// and per-server configuration loaded from <c>hlstats_Servers_Config</c>.
/// One instance exists per known server for as long as the daemon is running.
/// </summary>
public sealed class ServerState
{
    // --- Identity ---
    public int ServerId { get; init; }
    public string Address { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Game { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    // --- Live state ---
    public string CurrentMap { get; set; } = string.Empty;
    /// <summary>Unix timestamp of the last log event received from this server.</summary>
    public long LastEventUnix { get; set; }
    /// <summary>Unix timestamp of when the current map started (for MapStarted DB column).</summary>
    public long MapStartedUnix { get; set; }
    /// <summary>Unix timestamp of when the bonus round started (0 = no active bonus round).</summary>
    public long BonusRoundStartUnix { get; set; }
    public bool InBonusRound { get; set; }

    // --- Per-server config (populated from hlstats_Servers_Config) ---
    public ServerConfig Config { get; set; } = new();

    // --- CTF flag carry-forward (Perl: lastredflagdefend / lastblueflagdefend) ---
    // Set when a player triggers "flagevent_defended"; consumed when "flagevent_dropped"
    // fires within 1 second — the drop is reclassified as "flagevent_dropped_death".
    public long LastRedFlagDefendUnix { get; set; }
    public long LastBlueFlagDefendUnix { get; set; }

    // --- Kill carry-forward flags (Perl: nextkill* server globals) ---
    // Set by preceding events; consumed and reset on the next kill.

    // nextkillheadshot: DB player ID of the player whose next kill is a headshot (0 = none).
    // Set by a "Player triggered headshot" action event (plugin-based HS signalling).
    public int NextKillHeadshot { get; set; }

    // nextkillx / nextkillvicx: positions from "World triggered killlocation" (DoD:S).
    public int? NextKillX { get; set; }
    public int? NextKillY { get; set; }
    public int? NextKillZ { get; set; }
    public int? NextKillVicX { get; set; }
    public int? NextKillVicY { get; set; }
    public int? NextKillVicZ { get; set; }

    // --- Player registry ---
    // Key: "userid/uniqueid" — matches the Perl daemon's player hash key.
    private readonly Dictionary<string, PlayerSession> _players = new();

    public IReadOnlyDictionary<string, PlayerSession> Players => _players;

    /// <summary>Adds or replaces a player session.</summary>
    public void AddPlayer(PlayerSession session)
        => _players[$"{session.UserId}/{session.UniqueId}"] = session;

    /// <summary>Returns the player session keyed by <c>"userid/uniqueid"</c>, or null.</summary>
    public PlayerSession? LookupPlayer(int userId, string uniqueId)
        => _players.TryGetValue($"{userId}/{uniqueId}", out var p) ? p : null;

    /// <summary>Finds any player session whose unique ID matches, regardless of user ID.</summary>
    public PlayerSession? FindByUniqueId(string uniqueId)
    {
        foreach (var p in _players.Values)
            if (p.UniqueId == uniqueId) return p;
        return null;
    }

    /// <summary>Removes the player session and returns it, or returns null if not found.</summary>
    public PlayerSession? RemovePlayer(int userId, string uniqueId)
    {
        var key = $"{userId}/{uniqueId}";
        if (_players.TryGetValue(key, out var session))
        {
            _players.Remove(key);
            return session;
        }
        return null;
    }

    public int PlayerCount => _players.Count;

    /// <summary>
    /// Returns <see langword="true"/> when the bonus round is currently active and
    /// the server config says to ignore events during it.
    /// </summary>
    public bool IsInIgnoredBonusRound(long nowUnix)
    {
        if (!Config.BonusRoundIgnore || BonusRoundStartUnix == 0) return false;
        if (Config.BonusRoundTime <= 0) return InBonusRound;
        return InBonusRound && (nowUnix - BonusRoundStartUnix) <= Config.BonusRoundTime;
    }

    /// <summary>Removes all player sessions (e.g. on map change).</summary>
    public void ClearPlayers() => _players.Clear();
}
