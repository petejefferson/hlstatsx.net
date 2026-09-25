using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EntityServerConfig = HLStatsX.NET.Core.Entities.ServerConfig;

namespace HLStatsX.NET.Daemon.State;

/// <summary>
/// Central in-memory registry of all known game servers and the global daemon
/// options loaded from <c>hlstats_Options</c>.
/// </summary>
/// <remarks>
/// On startup (and on config reload) <see cref="LoadAsync"/> reads all server
/// rows and their per-server configs from the database, building <see cref="ServerState"/>
/// instances. During operation, UDP packets are mapped to a <see cref="ServerState"/>
/// by <c>"address:port"</c>.
/// </remarks>
public sealed class DaemonStateManager
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DaemonStateManager> _logger;

    // Key: "address:port" — matches the Perl daemon's $s_addr format.
    private readonly Dictionary<string, ServerState> _servers = new(StringComparer.OrdinalIgnoreCase);

    public DaemonOptions Options { get; private set; } = new();

    /// <summary>
    /// Game code used when auto-registering unknown servers, read from
    /// <c>Daemon:AutoRegisterGame</c> in appsettings. Empty string means disabled.
    /// </summary>
    public string AutoRegisterGame => _config["Daemon:AutoRegisterGame"] ?? string.Empty;

    public DaemonStateManager(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        IConfiguration config,
        ILogger<DaemonStateManager> logger)
    {
        _dbFactory = dbFactory;
        _config    = config;
        _logger    = logger;
    }

    /// <summary>
    /// Loads all server configs and global options from the database.
    /// Safe to call on startup and on SIGHUP (reload). Existing player sessions
    /// are preserved across a reload.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Load global options from hlstats_Options (opttype <= 1 matches the Perl filter)
        var options = await db.Options
            .Where(o => o.OptType <= 1)
            .ToListAsync(ct);

        ApplyGlobalOptions(options);

        // Appsettings override: Daemon:UseTimestamp=true/false takes precedence over the DB value.
        // Useful for stdin replay without touching the database.
        if (bool.TryParse(_config["Daemon:UseTimestamp"], out var useTimestampOverride))
            Options.UseTimestamp = useTimestampOverride;

        // Load all servers + their config rows in two queries (avoids N+1)
        var servers = await db.Servers.ToListAsync(ct);
        var configs = await db.ServerConfigs
            .ToListAsync(ct);

        var configsByServer = configs
            .GroupBy(c => c.ServerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Clear old server states but keep player sessions for servers that still exist
        var existingServers = _servers.Keys.ToList();

        foreach (var srv in servers)
        {
            var addr = $"{srv.Address}:{srv.Port}";

            if (!_servers.TryGetValue(addr, out var state))
            {
                state = new ServerState
                {
                    ServerId = srv.ServerId,
                    Address  = srv.Address,
                    Port     = srv.Port,
                    Game     = srv.Game,
                    Name     = srv.Name,
                };
                _servers[addr] = state;
            }

            // Always refresh config (the reload path needs this)
            state.Config = BuildServerConfig(
                configsByServer.TryGetValue(srv.ServerId, out var cfgList) ? cfgList : [],
                srv.RconPassword);
        }

        // Remove servers no longer in DB
        var currentAddrs = servers.Select(s => $"{s.Address}:{s.Port}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in existingServers.Where(a => !currentAddrs.Contains(a)))
            _servers.Remove(stale);

        _logger.LogInformation("Loaded {Count} servers from database.", servers.Count);
    }

    /// <summary>
    /// Returns the <see cref="ServerState"/> for the given <c>"address:port"</c>,
    /// or <see langword="null"/> when unknown.
    /// </summary>
    public ServerState? GetServer(string addr) =>
        _servers.TryGetValue(addr, out var s) ? s : null;

    /// <summary>All currently known server states.</summary>
    public IEnumerable<ServerState> AllServers => _servers.Values;

    /// <summary>
    /// Inserts a new server row for <paramref name="senderAddr"/>, copies default config
    /// from <c>hlstats_Games_Defaults</c> for the configured game, then reloads state.
    /// Returns the newly registered <see cref="ServerState"/>, or <see langword="null"/>
    /// when <c>Daemon:AutoRegisterGame</c> is not set or the address cannot be parsed.
    /// Mirrors <c>addServerToDB</c> in <c>hlstats.pl</c>.
    /// </summary>
    public async Task<ServerState?> AutoRegisterServerAsync(string senderAddr, CancellationToken ct = default)
    {
        var game = AutoRegisterGame;
        if (string.IsNullOrEmpty(game))
        {
            _logger.LogDebug(
                "Unknown server {Addr} — set Daemon:AutoRegisterGame in appsettings to enable auto-registration.",
                senderAddr);
            return null;
        }

        if (!TryParseSenderAddr(senderAddr, out var address, out var port))
        {
            _logger.LogWarning("Cannot parse address:port from '{Addr}' — skipping auto-registration.", senderAddr);
            return null;
        }

        await using var db = _dbFactory.CreateDbContext();

        var server = new Server
        {
            Address    = address,
            Port       = port,
            Name       = $"{address}:{port}",
            Game       = game,
            ActPlayers = 0,
            MaxPlayers = 0,
            ActMap     = string.Empty,
        };
        db.Servers.Add(server);
        await db.SaveChangesAsync(ct);

        // Copy game defaults into per-server config (mirrors Perl addServerToDB)
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO hlstats_Servers_Config (serverId, parameter, value)
            SELECT {server.ServerId}, parameter, value
            FROM hlstats_Games_Defaults WHERE code = {game}
            ON DUPLICATE KEY UPDATE value = VALUES(value)
            """, ct);

        // Explicit empty Mod row (vanilla — no mod override)
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO hlstats_Servers_Config (serverId, parameter, value) VALUES ({server.ServerId}, 'Mod', '') ON DUPLICATE KEY UPDATE value = value",
            ct);

        _logger.LogInformation(
            "Auto-registered server {Addr} with game '{Game}' (serverId={Id}).",
            senderAddr, game, server.ServerId);

        await LoadAsync(ct);
        return GetServer(senderAddr);
    }

    // --- Private helpers ---

    private void ApplyGlobalOptions(List<Option> options)
    {
        var lookup = options.ToDictionary(o => o.KeyName, o => o.Value, StringComparer.OrdinalIgnoreCase);

        string Get(string key, string def) =>
            lookup.TryGetValue(key, out var v) ? v : def;

        bool GetBool(string key, bool def) =>
            lookup.TryGetValue(key, out var v) ? v != "0" : def;

        int GetInt(string key, int def) =>
            lookup.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : def;

        Options = new DaemonOptions
        {
            Mode              = Get("Mode", "Normal"),
            UseTimestamp      = GetBool("UseTimestamp", false),
            DnsResolveIp      = GetBool("DNSResolveIP", true),
            DnsTimeoutSeconds = GetInt("DNSTimeout", 5),
            SkillMaxChange    = GetInt("SkillMaxChange", 100),
            SkillMinChange    = GetInt("SkillMinChange", 2),
            SkillRatioCap     = GetBool("SkillRatioCap", false),
            PlayerMinKills    = GetInt("PlayerMinKills", 50),
            AllowOnlyConfigServers = GetBool("AllowOnlyConfigServers", true),
            TrackStatsTrend   = GetBool("TrackStatsTrend", false),
            GlobalBanning     = GetBool("GlobalBanning", false),
            LogChat           = GetBool("LogChat", false),
            LogChatAdmins     = GetBool("LogChatAdmins", false),
            GlobalChat        = GetInt("GlobalChat", 0),
            RankingType       = Get("rankingtype", "skill"),
            UseGeoIpBinary    = GetBool("UseGeoIPBinary", false),
        };
    }

    private static Configuration.ServerConfig BuildServerConfig(List<EntityServerConfig> rows, string rconPassword)
    {
        var lookup = rows.ToDictionary(r => r.ConfigKey, r => r.ConfigValue, StringComparer.OrdinalIgnoreCase);

        string Get(string key, string def) =>
            lookup.TryGetValue(key, out var v) ? v : def;
        bool GetBool(string key, bool def) =>
            lookup.TryGetValue(key, out var v) ? v != "0" : def;
        int GetInt(string key, int def) =>
            lookup.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : def;

        return new Configuration.ServerConfig
        {
            MinPlayers               = GetInt("MinPlayers", 6),
            BroadcastEvents          = GetBool("BroadCastEvents", false),
            BroadcastPlayerActions   = GetBool("BroadCastPlayerActions", false),
            BroadcastEventsCommand   = Get("BroadCastEventsCommand", "say"),
            BroadcastEventsCommandAnnounce = Get("BroadCastEventsCommandAnnounce", "say"),
            PlayerEvents             = GetBool("PlayerEvents", true),
            PlayerEventsCommand      = Get("PlayerEventsCommand", "say"),
            PlayerEventsCommandOsd   = Get("PlayerEventsCommandOSD", ""),
            PlayerEventsCommandHint  = Get("PlayerEventsCommandHint", ""),
            PlayerEventsAdminCommand = Get("PlayerEventsAdminCommand", ""),
            ShowStats                = GetBool("ShowStats", true),
            AutoTeamBalance          = GetBool("AutoTeamBalance", false),
            AutoBanRetry             = GetBool("AutoBanRetry", false),
            TrackServerLoad          = GetBool("TrackServerLoad", false),
            MinimumPlayersRank       = GetInt("MinimumPlayersRank", 0),
            Admins                   = Get("Admins", ""),
            SwitchAdmins             = GetBool("SwitchAdmins", false),
            IgnoreBots               = GetBool("IgnoreBots", true),
            SkillMode                = GetInt("SkillMode", 0),
            GameType                 = GetInt("GameType", 0),
            BonusRoundTime           = GetInt("BonusRoundTime", 0),
            BonusRoundIgnore         = GetBool("BonusRoundIgnore", false),
            Mod                      = Get("Mod", ""),
            EnablePublicCommands     = GetBool("EnablePublicCommands", true),
            ConnectAnnounce          = GetBool("ConnectAnnounce", true),
            UpdateHostname           = GetBool("UpdateHostname", false),
            DefaultDisplayEvents     = GetBool("DefaultDisplayEvents", true),
            TkPenalty                = GetInt("TKPenalty", 50),
            SuicidePenalty           = GetInt("SuicidePenalty", 5),
            GameEngine               = GetInt("GameEngine", 2),
            RconPassword             = rconPassword,
            MinRank                  = GetInt("MinimumPlayersRank", 0),
        };
    }

    /// <summary>
    /// Splits a <c>"address:port"</c> string (e.g. <c>"192.168.1.1:27015"</c>) into its
    /// components. Uses the last colon so IPv6 bracket notation (<c>"[::1]:27015"</c>) is
    /// handled correctly.
    /// </summary>
    internal static bool TryParseSenderAddr(string senderAddr, out string address, out int port)
    {
        address = string.Empty;
        port    = 0;

        var lastColon = senderAddr.LastIndexOf(':');
        if (lastColon <= 0) return false;
        if (!int.TryParse(senderAddr.AsSpan(lastColon + 1), out port)) return false;

        address = senderAddr[..lastColon];
        return !string.IsNullOrEmpty(address);
    }
}
