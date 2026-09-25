namespace HLStatsX.NET.Awards.Services;

/// <summary>
/// Performs reverse DNS lookups for player IPs stored in <c>hlstats_Events_Connects</c>,
/// then classifies each resolved hostname into a host group.
/// Mirrors <c>hlstats-resolve.pl</c>.
/// </summary>
public interface IDnsResolveService
{
    /// <summary>
    /// Resolves all unresolved IP addresses in <c>hlstats_Events_Connects</c> and writes
    /// the <c>hostname</c> and <c>hostgroup</c> columns. IPs that already have a hostname
    /// or that time out are skipped/failed and retried on the next run.
    /// </summary>
    Task<DnsResolveResult> ResolveAsync(CancellationToken ct = default);
}

/// <summary>Result of a DNS resolve pass.</summary>
/// <param name="IpsResolved">IPs successfully resolved and written to the database.</param>
/// <param name="IpsFailed">IPs where the DNS lookup timed out or returned no PTR record.</param>
/// <param name="IpsSkipped">IPs that were blank or otherwise not actionable.</param>
public record DnsResolveResult(int IpsResolved, int IpsFailed, int IpsSkipped);
