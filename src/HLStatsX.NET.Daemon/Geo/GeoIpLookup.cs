using HLStatsX.NET.Infrastructure.Data;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

namespace HLStatsX.NET.Daemon.Geo;

/// <summary>
/// Real-time GeoIP lookup on player connect using a MaxMind GeoLite2-City MMDB file.
/// Mirrors <c>setGeoData</c> in <c>HLstats_Player.pm</c> when <c>UseGeoIPBinary = 1</c>.
/// </summary>
/// <remarks>
/// The MMDB file is opened once on first use and reused for the process lifetime.
/// The Awards worker handles the batch <c>DoGeoIP</c> pass for players without geo data;
/// this class handles the live per-connect path.
/// </remarks>
public sealed class GeoIpLookup : IDisposable
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<GeoIpLookup> _logger;
    private DatabaseReader? _reader;
    private bool _initAttempted;

    public GeoIpLookup(IDbContextFactory<HLStatsDbContext> dbFactory, ILogger<GeoIpLookup> logger)
    {
        _dbFactory = dbFactory;
        _logger    = logger;
    }

    /// <summary>
    /// Looks up <paramref name="ipAddress"/> and writes geo data to
    /// <c>hlstats_Players</c> for <paramref name="playerId"/>.
    /// No-ops if the MMDB is not open, the IP is unroutable, or no record is found.
    /// </summary>
    public async Task LookupAndUpdateAsync(
        int playerId,
        string ipAddress,
        string dbPath,
        CancellationToken ct = default)
    {
        if (playerId <= 0 || string.IsNullOrWhiteSpace(ipAddress)) return;

        if (!IPAddress.TryParse(ipAddress, out var ip) ||
            ip.AddressFamily != AddressFamily.InterNetwork)
            return;

        var reader = GetReader(dbPath);
        if (reader is null) return;

        string flag, country, city, state;
        float? lat, lng;

        try
        {
            var response = reader.City(ipAddress);
            flag    = response.Country.IsoCode ?? "";
            country = response.Country.Name ?? "";
            city    = response.City.Name ?? "";
            state   = response.MostSpecificSubdivision.Name ?? "";
            lat     = (float?)response.Location.Latitude;
            lng     = (float?)response.Location.Longitude;
        }
        catch (AddressNotFoundException) { return; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {Ip}.", ipAddress);
            return;
        }

        // Perl: only write when $lng is defined (i.e. we got coordinates)
        if (lng is null) return;

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE hlstats_Players SET
              flag    = {0},
              country = {1},
              city    = {2},
              state   = {3},
              lat     = {4},
              lng     = {5}
            WHERE playerId = {6}
            """,
            new object[] { flag, country, city, state,
                           (object?)lat ?? null!, (object?)lng ?? null!, playerId },
            ct);

        _logger.LogDebug("GeoIP: player {Id} from {Ip} → {Flag} {City}.", playerId, ipAddress, flag, city);
    }

    private DatabaseReader? GetReader(string dbPath)
    {
        if (_reader is not null) return _reader;
        if (_initAttempted) return null;

        _initAttempted = true;

        if (!File.Exists(dbPath))
        {
            _logger.LogWarning("GeoLite2-City MMDB not found at '{Path}' — GeoIP disabled.", dbPath);
            return null;
        }

        try
        {
            _reader = new DatabaseReader(dbPath);
            _logger.LogInformation("GeoLite2-City MMDB loaded from '{Path}'.", dbPath);
            return _reader;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GeoLite2-City MMDB at '{Path}'.", dbPath);
            return null;
        }
    }

    public void Dispose() => _reader?.Dispose();
}
