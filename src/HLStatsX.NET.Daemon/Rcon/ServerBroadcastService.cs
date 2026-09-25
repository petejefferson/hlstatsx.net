using System.Collections.Concurrent;
using HLStatsX.NET.Daemon.Network;
using HLStatsX.NET.Daemon.State;

namespace HLStatsX.NET.Daemon.Rcon;

/// <summary>
/// Central RCON broadcasting service. Manages one <see cref="IRconClient"/> per server,
/// instantiating <see cref="SourceRconClient"/> or <see cref="GoldSrcRconClient"/> based on
/// <see cref="Configuration.ServerConfig.GameEngine"/>. Mirrors the Perl daemon's
/// <c>dorcon</c>, <c>messageAll</c>, and <c>messageMany</c> methods in <c>HLstats_Server.pm</c>.
/// RCON is automatically disabled in stdin/file replay mode (mirrors Perl --stdin implies --norcon).
/// </summary>
public sealed class ServerBroadcastService
{
    private readonly ConcurrentDictionary<string, IRconClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ServerBroadcastService> _logger;
    private readonly Func<ServerState, IRconClient>? _clientFactory;
    private readonly bool _rconEnabled;

    public ServerBroadcastService(ILoggerFactory loggerFactory, ILogger<ServerBroadcastService> logger, UdpListener udpListener)
    {
        _loggerFactory = loggerFactory;
        _logger        = logger;
        _rconEnabled   = !udpListener.IsStdinMode;
    }

    internal ServerBroadcastService(
        ILoggerFactory loggerFactory,
        ILogger<ServerBroadcastService> logger,
        Func<ServerState, IRconClient> clientFactory)
    {
        _loggerFactory = loggerFactory;
        _logger        = logger;
        _clientFactory = clientFactory;
        _rconEnabled   = true;
    }

    /// <summary>
    /// Sends a broadcast message to all players on the server via RCON.
    /// Only executed when <see cref="Configuration.ServerConfig.BroadcastEvents"/> is true,
    /// unless <paramref name="force"/> is <see langword="true"/>.
    /// Mirrors Perl <c>messageAll</c> in <c>HLstats_Server.pm</c>.
    /// </summary>
    public async Task MessageAllAsync(ServerState server, string message, bool force = false, CancellationToken ct = default)
    {
        if (!force && !server.Config.BroadcastEvents) return;
        var cmd = $"{server.Config.BroadcastEventsCommandAnnounce} \"{Sanitize(message)}\"";
        await ExecuteAsync(server, cmd, ct);
    }

    /// <summary>
    /// Sends a message to a specific player via RCON using their server user ID.
    /// Only executed when both <see cref="Configuration.ServerConfig.BroadcastEvents"/> and
    /// <see cref="PlayerSession.DisplayEvents"/> are true.
    /// </summary>
    public async Task MessagePlayerAsync(ServerState server, PlayerSession player, string message, CancellationToken ct = default)
    {
        if (!server.Config.BroadcastEvents) return;
        if (!player.DisplayEvents) return;
        if (player.IsBot || player.UserId <= 0) return;
        var cmd = $"{server.Config.PlayerEventsCommand} \"{player.UserId}\" \"{Sanitize(message)}\"";
        await ExecuteAsync(server, cmd, ct);
    }

    /// <summary>
    /// Sends a raw RCON command to the server.
    /// </summary>
    public async Task ExecuteAsync(ServerState server, string command, CancellationToken ct = default)
    {
        if (!_rconEnabled) return;
        var client = GetOrCreateClient(server);
        if (client is null) return;

        _logger.LogDebug("RCON {Server}: {Command}", $"{server.Address}:{server.Port}", command);
        try
        {
            await client.ExecuteAsync(command, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RCON command failed on {Server}.", $"{server.Address}:{server.Port}");
        }
    }

    /// <summary>
    /// Kicks a player from the server via RCON.
    /// </summary>
    public async Task KickPlayerAsync(ServerState server, PlayerSession player, string reason, CancellationToken ct = default)
    {
        string cmd = server.Config.GameEngine == 1
            ? $"kick #{player.UserId}"
            : $"kickid {player.UserId} {Sanitize(reason)}";
        await ExecuteAsync(server, cmd, ct);
    }

    /// <summary>
    /// Disposes the RCON client for a server that is being removed or reloaded.
    /// </summary>
    public async ValueTask DisposeServerClientAsync(string serverKey)
    {
        if (_clients.TryRemove(serverKey, out var client))
            await client.DisposeAsync();
    }

    private IRconClient? GetOrCreateClient(ServerState server)
    {
        var key = $"{server.Address}:{server.Port}";
        return _clients.GetOrAdd(key, _ => _clientFactory is not null
            ? _clientFactory(server)
            : CreateClient(server));
    }

    private IRconClient CreateClient(ServerState server)
    {
        if (server.Config.GameEngine == 1)
        {
            return new GoldSrcRconClient(
                server.Address,
                server.Port,
                server.Config.RconPassword,
                _loggerFactory.CreateLogger<GoldSrcRconClient>());
        }
        return new SourceRconClient(
            server.Address,
            server.Port,
            server.Config.RconPassword,
            _loggerFactory.CreateLogger<SourceRconClient>());
    }

    private static string Sanitize(string message)
        => message.Replace(";", string.Empty, StringComparison.Ordinal);
}
