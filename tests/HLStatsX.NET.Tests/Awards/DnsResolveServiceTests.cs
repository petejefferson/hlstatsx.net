using HLStatsX.NET.Awards.Services;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Awards;

public class DnsResolveServiceTests
{
    private static IConfiguration DefaultConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HLStatsX:Resolve:DnsTimeoutSeconds"] = "5" })
            .Build();

    private static TestDbContextFactory EmptyDb()
    {
        var opts = new DbContextOptionsBuilder<HLStatsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(opts);
    }

    // ── No unresolved IPs ────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_EmptyDatabase_ReturnsZeroCounts()
    {
        var svc = new DnsResolveService(EmptyDb(), DefaultConfig(), NullLogger<DnsResolveService>.Instance);

        var result = await svc.ResolveAsync();

        result.IpsResolved.Should().Be(0);
        result.IpsFailed.Should().Be(0);
        result.IpsSkipped.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_AllIpsAlreadyHaveHostname_ReturnsZeroCounts()
    {
        var db = EmptyDb();
        await using (var ctx = db.CreateDbContext())
        {
            ctx.EventConnects.AddRange(
                new EventConnect { IpAddress = "1.2.3.4", Hostname = "host.isp.net" },
                new EventConnect { IpAddress = "5.6.7.8", Hostname = "other.example.com" });
            await ctx.SaveChangesAsync();
        }

        var svc = new DnsResolveService(db, DefaultConfig(), NullLogger<DnsResolveService>.Instance);

        var result = await svc.ResolveAsync();

        result.IpsResolved.Should().Be(0);
        result.IpsFailed.Should().Be(0);
        result.IpsSkipped.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_BlankIpAddress_CountsAsSkipped()
    {
        var db = EmptyDb();
        await using (var ctx = db.CreateDbContext())
        {
            ctx.EventConnects.Add(new EventConnect { IpAddress = "", Hostname = "" });
            await ctx.SaveChangesAsync();
        }

        var svc = new DnsResolveService(db, DefaultConfig(), NullLogger<DnsResolveService>.Instance);

        var result = await svc.ResolveAsync();

        result.IpsSkipped.Should().Be(1);
        result.IpsResolved.Should().Be(0);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// HostGroupClassifier tests (pure logic — no DB or DNS required)
// ────────────────────────────────────────────────────────────────────────────

public class HostGroupClassifierTests
{
    // ── Empty hostname ───────────────────────────────────────────────────────

    [Fact]
    public void Classify_EmptyHostname_ReturnsEmpty()
    {
        var c = new HostGroupClassifier([]);
        c.Classify("").Should().Be("");
    }

    // ── Custom host group patterns ───────────────────────────────────────────

    [Fact]
    public void Classify_ExactPatternMatch_ReturnsGroupName()
    {
        var c = new HostGroupClassifier([(".isp.net", "My ISP")]);
        c.Classify("host.isp.net").Should().Be("My ISP");
    }

    [Fact]
    public void Classify_GlobPatternMatchesMidSegment()
    {
        // *.isp.net  →  [^.]*\.isp\.net$  — matches any single segment before isp.net
        var c = new HostGroupClassifier([("*.isp.net", "My ISP")]);
        c.Classify("adsl123.isp.net").Should().Be("My ISP");
        c.Classify("dialup.isp.net").Should().Be("My ISP");
    }

    [Fact]
    public void Classify_GlobDoesNotCrossDotBoundary()
    {
        // *.net should NOT match 'host.isp.com' (wrong TLD)
        var c = new HostGroupClassifier([("*.net", "Some Net")]);
        c.Classify("host.isp.com").Should().NotBe("Some Net");
    }

    [Fact]
    public void Classify_LongerPatternTriedFirst()
    {
        // Patterns sorted longest-first by the caller; classifier preserves order
        var c = new HostGroupClassifier([
            (".adsl.isp.net", "ISP ADSL"),
            (".isp.net",      "ISP General"),
        ]);
        c.Classify("host.adsl.isp.net").Should().Be("ISP ADSL");
    }

    [Fact]
    public void Classify_PatternMatchIsCaseInsensitive()
    {
        var c = new HostGroupClassifier([(".ISP.NET", "My ISP")]);
        c.Classify("host.isp.net").Should().Be("My ISP");
    }

    // ── Fallback domain extraction ───────────────────────────────────────────

    [Theory]
    [InlineData("max1.xyz.someisp.net",  "someisp.net")]   // 3-letter gTLD → 2 parts
    [InlineData("max1.xyz.someisp.com",  "someisp.com")]   // .com → 2 parts
    [InlineData("max1.xyz.someisp.org",  "someisp.org")]   // .org → 2 parts
    [InlineData("host.isp.edu",          "isp.edu")]       // .edu → 2 parts
    public void ExtractDomainGroup_ThreeLetterTld_ReturnsTwoParts(string hostname, string expected)
    {
        HostGroupClassifier.ExtractDomainGroup(hostname).Should().Be(expected);
    }

    [Theory]
    [InlineData("max1.xyz.someisp.de",  "someisp.de")]   // Germany — no SLD
    [InlineData("host.provider.ru",     "provider.ru")]  // Russia — no SLD
    [InlineData("a.b.provider.nl",      "provider.nl")]  // Netherlands — no SLD
    public void ExtractDomainGroup_NoSldCountry_ReturnsTwoParts(string hostname, string expected)
    {
        HostGroupClassifier.ExtractDomainGroup(hostname).Should().Be(expected);
    }

    [Theory]
    [InlineData("max1.xyz.someisp.co.uk",  "someisp.co.uk")]   // UK with SLD
    [InlineData("host.isp.com.au",         "isp.com.au")]      // Australia with SLD
    [InlineData("host.net.nz",             "host.net.nz")]     // NZ (net.nz is the SLD+ccTLD)
    public void ExtractDomainGroup_CcTldWithSld_ReturnsThreeParts(string hostname, string expected)
    {
        HostGroupClassifier.ExtractDomainGroup(hostname).Should().Be(expected);
    }

    [Fact]
    public void ExtractDomainGroup_SingleSegment_ReturnsFull()
    {
        HostGroupClassifier.ExtractDomainGroup("localhost").Should().Be("localhost");
    }

    [Fact]
    public void Classify_NoPatterns_FallsBackToDomainExtraction()
    {
        var c = new HostGroupClassifier([]);
        c.Classify("pc42.broadband.isp.net").Should().Be("isp.net");
    }
}

// ────────────────────────────────────────────────────────────────────────────
// MaintenanceTaskFlags — resolve flag
// ────────────────────────────────────────────────────────────────────────────

public class MaintenanceTaskFlagsResolveTests
{
    [Fact]
    public void Default_DoesNotIncludeResolve()
    {
        HLStatsX.NET.Awards.Workers.MaintenanceTaskFlags.Default.Resolve.Should().BeFalse();
    }

    [Fact]
    public void All_IncludesResolve()
    {
        HLStatsX.NET.Awards.Workers.MaintenanceTaskFlags.All.Resolve.Should().BeTrue();
    }

    [Fact]
    public void FromArgs_ResolveFlag_EnablesOnlyResolve()
    {
        var f = HLStatsX.NET.Awards.Workers.MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--resolve"]);
        f.Resolve.Should().BeTrue();
        f.Inactive.Should().BeFalse();
        f.Awards.Should().BeFalse();
        f.GeoIp.Should().BeFalse();
    }

    [Fact]
    public void ToString_WithResolve_ContainsResolve()
    {
        var f = HLStatsX.NET.Awards.Workers.MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--resolve"]);
        f.ToString().Should().Contain("resolve");
    }
}
