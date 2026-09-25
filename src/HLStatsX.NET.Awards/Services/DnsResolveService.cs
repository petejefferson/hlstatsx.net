using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace HLStatsX.NET.Awards.Services;

// Replicates hlstats-resolve.pl (default mode — no --regroup):
// - loads DISTINCT unresolved IPs from hlstats_Events_Connects (hostname = '')
// - performs a reverse DNS lookup for each IP with a configurable timeout
// - calculates the host group via hlstats_HostGroups patterns + domain heuristic
// - UPDATEs hlstats_Events_Connects SET hostname, hostgroup WHERE ipAddress = IP
// IPs that time out or have no PTR record are skipped and retried on the next run.
public class DnsResolveService : IDnsResolveService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<DnsResolveService> _logger;

    public DnsResolveService(
        IDbContextFactory<HLStatsDbContext> factory,
        IConfiguration config,
        ILogger<DnsResolveService> logger)
    {
        _factory = factory;
        _config  = config;
        _logger  = logger;
    }

    /// <summary>
    /// Resolves unresolved IPs in <c>hlstats_Events_Connects</c> and writes hostname +
    /// hostgroup. Mirrors the default (non-<c>--regroup</c>) mode of <c>hlstats-resolve.pl</c>.
    /// </summary>
    public async Task<DnsResolveResult> ResolveAsync(CancellationToken ct = default)
    {
        var timeoutSeconds = _config.GetValue<int>("HLStatsX:Resolve:DnsTimeoutSeconds", 5);

        await using var db = _factory.CreateDbContext();

        // Load host groups — sort longest-first in memory (avoids translating LENGTH() to SQL)
        var rawGroups = await db.HostGroups
            .Select(h => new { h.Pattern, h.Name })
            .ToListAsync(ct);

        var patterns = rawGroups
            .OrderByDescending(h => h.Pattern.Length)
            .ThenBy(h => h.Pattern)
            .Select(h => (h.Pattern, h.Name))
            .ToList();

        var classifier = new HostGroupClassifier(patterns);

        // DISTINCT unresolved IPs
        var unresolvedIps = await db.EventConnects
            .Where(e => e.Hostname == "")
            .Select(e => e.IpAddress)
            .Distinct()
            .ToListAsync(ct);

        if (unresolvedIps.Count == 0)
        {
            _logger.LogInformation("No unresolved IPs found in hlstats_Events_Connects");
            return new DnsResolveResult(0, 0, 0);
        }

        _logger.LogInformation("Resolving {Count} unresolved IP addresses (timeout {Sec}s)",
            unresolvedIps.Count, timeoutSeconds);

        int resolved = 0, failed = 0, skipped = 0;

        foreach (var ip in unresolvedIps)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(ip))
            {
                skipped++;
                continue;
            }

            string hostname;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var entry = await System.Net.Dns.GetHostEntryAsync(ip, cts.Token);
                hostname  = entry.HostName;
                _logger.LogDebug("{Ip} → {Hostname}", ip, hostname);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogDebug("DNS timeout for {Ip}", ip);
                failed++;
                continue;
            }
            catch (SocketException)
            {
                _logger.LogDebug("No PTR record for {Ip}", ip);
                failed++;
                continue;
            }

            var hostgroup = classifier.Classify(hostname);

            await db.Database.ExecuteSqlAsync(
                $"UPDATE hlstats_Events_Connects SET hostname={hostname}, hostgroup={hostgroup} WHERE ipAddress={ip}",
                ct);

            resolved++;
        }

        _logger.LogInformation(
            "DNS resolve complete — {Resolved} resolved, {Failed} failed/timeout, {Skipped} skipped",
            resolved, failed, skipped);

        return new DnsResolveResult(resolved, failed, skipped);
    }
}
