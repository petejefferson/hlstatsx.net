using HLStatsX.NET.Daemon.Geo;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Daemon.Geo;

/// <summary>
/// Tests for the early-exit guards in <see cref="GeoIpLookup.LookupAndUpdateAsync"/>.
/// Full integration paths (actual MMDB lookup) require the MaxMind binary file
/// and are not exercised here.
/// </summary>
public sealed class GeoIpLookupTests
{
    private static Mock<IDbContextFactory<HLStatsDbContext>> MockFactory() => new();

    // ── Guard: invalid playerId ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task LookupAndUpdateAsync_InvalidPlayerId_ReturnsWithoutOpeningDb(int playerId)
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(playerId, "1.2.3.4", "any.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── Guard: invalid IP address ─────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LookupAndUpdateAsync_EmptyOrNullIp_ReturnsWithoutOpeningDb(string? ipAddress)
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(1, ipAddress!, "any.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    public async Task LookupAndUpdateAsync_UnparsableIp_ReturnsWithoutOpeningDb(string ipAddress)
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(1, ipAddress, "any.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task LookupAndUpdateAsync_IPv6Address_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(1, "::1", "any.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task LookupAndUpdateAsync_IPv6FullAddress_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(1, "2001:db8::1", "any.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── Guard: MMDB file not found ────────────────────────────────────────────

    [Fact]
    public async Task LookupAndUpdateAsync_MmdbNotFound_ReturnsWithoutOpeningDb()
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        await svc.LookupAndUpdateAsync(1, "1.2.3.4", @"C:\nonexistent\GeoLite2-City.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task LookupAndUpdateAsync_MmdbNotFound_SubsequentCallsAlsoSkip()
    {
        var factory = MockFactory();
        using var svc = new GeoIpLookup(factory.Object, NullLogger<GeoIpLookup>.Instance);

        // First call — file missing, sets _initAttempted flag
        await svc.LookupAndUpdateAsync(1, "1.2.3.4", @"C:\nonexistent\file.mmdb");
        // Second call — _initAttempted is true, skips file check too
        await svc.LookupAndUpdateAsync(2, "5.6.7.8", @"C:\nonexistent\file.mmdb");

        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }
}
