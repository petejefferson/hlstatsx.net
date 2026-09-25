using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.Network;
using HLStatsX.NET.Daemon.Parsing;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Daemon.Trend;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace HLStatsX.NET.Daemon;

/// <summary>
/// Main hosted service. Wires the UDP listener → log parser → event router pipeline
/// and drives periodic tasks (trend tracking, config reload).
/// Mirrors the outer receive loop in <c>hlstats.pl</c>.
/// </summary>
public sealed class DaemonWorker : BackgroundService
{
    private readonly DaemonStateManager _stateManager;
    private readonly UdpListener _udpListener;
    private readonly EventRouter _router;
    private readonly TrendTracker _trendTracker;
    private readonly PlayerDbService _playerDb;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DaemonWorker> _logger;

    // Set by the SIGHUP handler; checked at the top of each packet loop iteration.
    private volatile bool _reloadRequested;

    public DaemonWorker(
        DaemonStateManager stateManager,
        UdpListener udpListener,
        EventRouter router,
        TrendTracker trendTracker,
        PlayerDbService playerDb,
        IDbContextFactory<HLStatsDbContext> dbFactory,
        IConfiguration config,
        IHostApplicationLifetime lifetime,
        ILogger<DaemonWorker> logger)
    {
        _stateManager = stateManager;
        _udpListener  = udpListener;
        _router       = router;
        _trendTracker = trendTracker;
        _playerDb     = playerDb;
        _dbFactory    = dbFactory;
        _config       = config;
        _lifetime     = lifetime;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register SIGHUP handler on POSIX platforms (Linux/macOS).
        // SIGHUP flushes all active player sessions then reloads server configs
        // and global options from the database — mirrors the Perl HUP_handler.
        // Windows does not support SIGHUP; registration is skipped silently.
        PosixSignalRegistration? sighupReg = null;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                sighupReg = PosixSignalRegistration.Create(PosixSignal.SIGHUP, ctx =>
                {
                    ctx.Cancel = true; // suppress default terminate behaviour
                    _reloadRequested = true;
                });
                _logger.LogInformation("SIGHUP handler registered — send SIGHUP to reload configuration.");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SIGHUP registration unavailable on this platform.");
            }
        }

        _logger.LogInformation("Loading server configurations from database...");
        await _stateManager.LoadAsync(stoppingToken);
        _logger.LogInformation("HLStatsX.NET Daemon started.");

        long lastTrendUnix = 0;

        await foreach (var packet in _udpListener.ListenAsync(stoppingToken))
        {
            // Handle pending SIGHUP reload before processing the next packet
            if (_reloadRequested)
            {
                _reloadRequested = false;
                await HandleReloadAsync(stoppingToken);
            }

            if (!LogLineParser.TryParse(packet.Raw, out var parsed))
            {
                _logger.LogDebug("Malformed packet from {Sender}: {Raw}", packet.SenderAddr, packet.Raw);
                continue;
            }

            var server = _stateManager.GetServer(packet.SenderAddr);
            if (server is null)
            {
                if (_stateManager.Options.AllowOnlyConfigServers)
                {
                    _logger.LogDebug("Unknown server {Addr} — skipped (AllowOnlyConfigServers=true).", packet.SenderAddr);
                    continue;
                }

                // Auto-register the server using the game from Daemon:AutoRegisterGame
                server = await _stateManager.AutoRegisterServerAsync(packet.SenderAddr, stoppingToken);
                if (server is null)
                    continue;
            }

            var options = _stateManager.Options;

            // In stdin/replay mode always use the log-file timestamp (mirrors Perl forcing $g_timestamp=1 in stdin).
            long nowUnix = (_udpListener.IsStdinMode || options.UseTimestamp)
                ? parsed.Timestamp.ToUnixTimeSeconds()
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Clear stale map state when no events for > 299 s (UDP mode only)
            if (!_udpListener.IsStdinMode &&
                server.LastEventUnix > 0 &&
                (nowUnix - server.LastEventUnix) > 299)
            {
                server.CurrentMap = string.Empty;
            }

            server.LastEventUnix = nowUnix;

            var ctx = new EventContext
            {
                Server    = server,
                EventUnix = nowUnix,
                Map       = server.CurrentMap,
                IsReplay  = _udpListener.IsStdinMode,
            };

            try
            {
                await _router.DispatchAsync(parsed.EventText, ctx, options, packet.SenderAddr, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event from {Server}: {Event}",
                    packet.SenderAddr, parsed.EventText);
            }

            // Trend tracking: every 299 seconds in UDP mode when enabled
            if (options.TrackStatsTrend && !_udpListener.IsStdinMode)
            {
                if (lastTrendUnix == 0 || nowUnix >= lastTrendUnix + 299)
                {
                    try
                    {
                        await _trendTracker.TrackAsync(nowUnix, stoppingToken);
                        lastTrendUnix = nowUnix;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Trend tracking failed.");
                    }
                }
            }
        }

        sighupReg?.Dispose();
        await FlushAllSessionsAsync();

        if (_udpListener.IsStdinMode)
        {
            await StampImportEndTimeAsync();
            _lifetime.StopApplication();
        }
    }

    /// <summary>
    /// Handles a SIGHUP: flushes all active sessions to DB, clears the in-memory
    /// player registry, then reloads server configs and global options.
    /// Mirrors <c>reloadConfiguration</c> → <c>flushAll</c> + <c>readDatabaseConfig</c>
    /// in <c>hlstats.pl</c>.
    /// </summary>
    private async Task HandleReloadAsync(CancellationToken ct)
    {
        _logger.LogInformation("SIGHUP — flushing active sessions and reloading configuration.");

        foreach (var server in _stateManager.AllServers)
        {
            long now = server.LastEventUnix > 0
                ? server.LastEventUnix
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var player in server.Players.Values.ToList())
            {
                try
                {
                    await _playerDb.FlushSessionAsync(player, now, _udpListener.IsStdinMode, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error flushing session for {Player} during reload.", player.Name);
                }
            }
            server.ClearPlayers();
        }

        await _stateManager.LoadAsync(ct);
        _logger.LogInformation("Configuration reloaded.");
    }

    private async Task FlushAllSessionsAsync()
    {
        _logger.LogInformation("Daemon stopping — flushing all player sessions.");

        foreach (var server in _stateManager.AllServers)
        {
            long now = server.LastEventUnix > 0
                ? server.LastEventUnix
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var player in server.Players.Values.ToList())
            {
                try
                {
                    await _playerDb.FlushSessionAsync(player, now, _udpListener.IsStdinMode, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing session for {Player}.", player.Name);
                }
            }
        }
    }

    /// <summary>
    /// Mirrors the Perl daemon's post-import <c>UPDATE hlstats_Players SET last_event=UNIX_TIMESTAMP()</c>.
    /// If <c>Daemon:ImportEndTime</c> is set, uses that unix timestamp instead of the current wall clock,
    /// allowing exact reproduction of a historical Perl import's final state.
    /// Skipped entirely if neither is provided (leaves per-player log timestamps intact).
    /// </summary>
    private async Task StampImportEndTimeAsync()
    {
        long endTime;
        var raw = _config["Daemon:ImportEndTime"];
        if (long.TryParse(raw, out var configured))
        {
            endTime = configured;
            _logger.LogInformation("Stamping last_event = {EndTime} (ImportEndTime) on all players.", endTime);
        }
        else
        {
            // No override supplied — leave per-player timestamps from FlushAllSessionsAsync intact.
            return;
        }

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Players SET last_event = {0}", endTime);
    }
}
