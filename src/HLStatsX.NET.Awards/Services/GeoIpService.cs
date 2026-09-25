using HLStatsX.NET.Infrastructure.Data;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace HLStatsX.NET.Awards.Services;

// Replicates DoGeoIP() from hlstats-awards.pl:
// - queries players with a known lastAddress but no flag (country code) set
// - looks up each IPv4 address in the GeoLite2-City MMDB
// - writes flag, country, city, state, lat, lng back to hlstats_Players
public class GeoIpService : IGeoIpService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(IDbContextFactory<HLStatsDbContext> factory, IConfiguration config, ILogger<GeoIpService> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public async Task<GeoIpResult> LookupAsync(CancellationToken ct = default)
    {
        var dbPath = _config["HLStatsX:GeoIP:DatabasePath"] ?? "";
        if (!File.Exists(dbPath))
        {
            _logger.LogWarning("GeoLite2-City database not found at '{Path}' — skipping GeoIP lookup", dbPath);
            return new GeoIpResult(0, 0, dbPath);
        }

        await using var db = _factory.CreateDbContext();

        var players = await db.Players
            .Where(p => p.Flag == "" && p.LastAddress != "")
            .Select(p => new { p.PlayerId, p.LastAddress, p.LastName })
            .ToListAsync(ct);

        if (players.Count == 0)
        {
            _logger.LogInformation("No players found needing GeoIP lookup");
            return new GeoIpResult(0, 0, dbPath);
        }

        _logger.LogInformation("Looking up GeoIP for {Count} players using {Path}", players.Count, dbPath);

        using var reader = new DatabaseReader(dbPath);
        int updated = 0, skipped = 0;

        foreach (var player in players)
        {
            ct.ThrowIfCancellationRequested();

            var address = player.LastAddress.Trim();
            if (!IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                skipped++;
                continue;
            }

            try
            {
                var response = reader.City(address);

                var flag        = response.Country.IsoCode ?? "";
                var countryName = response.Country.Name ?? "";
                var cityName    = response.City.Name ?? "";
                var stateName   = response.MostSpecificSubdivision.Name ?? "";
                float? lat      = (float?)response.Location.Latitude;
                float? lng      = (float?)response.Location.Longitude;

                if (string.IsNullOrEmpty(flag))
                {
                    skipped++;
                    continue;
                }

                await db.Database.ExecuteSqlAsync(
                    $"UPDATE hlstats_Players SET flag={flag}, country={countryName}, city={cityName}, state={stateName}, lat={lat}, lng={lng} WHERE playerId={player.PlayerId}",
                    ct);

                updated++;
                _logger.LogDebug("Updated player {Name} ({Ip}) → {Flag} {City}", player.LastName, address, flag, cityName);
            }
            catch (AddressNotFoundException)
            {
                skipped++;
            }
        }

        _logger.LogInformation("GeoIP lookup complete — {Updated} updated, {Skipped} skipped", updated, skipped);
        return new GeoIpResult(updated, skipped, dbPath);
    }
}
