using HLStatsX.NET.Awards.Services;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Awards;

public class GeoIpServiceTests
{
    private static IConfiguration Config(string dbPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HLStatsX:GeoIP:DatabasePath"] = dbPath })
            .Build();

    private static TestDbContextFactory EmptyDb()
    {
        var opts = new DbContextOptionsBuilder<HLStatsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(opts);
    }

    // ── Early-exit: file not available ────────────────────────────────────────

    [Fact]
    public async Task LookupAsync_EmptyDatabasePath_SkipsWithoutTouchingDb()
    {
        var factory = new Mock<IDbContextFactory<HLStatsDbContext>>();
        var svc = new GeoIpService(factory.Object, Config(""), NullLogger<GeoIpService>.Instance);

        var result = await svc.LookupAsync();

        result.PlayersUpdated.Should().Be(0);
        result.PlayersSkipped.Should().Be(0);
        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    [Fact]
    public async Task LookupAsync_DatabaseFileNotFound_SkipsWithoutTouchingDb()
    {
        var factory = new Mock<IDbContextFactory<HLStatsDbContext>>();
        var svc = new GeoIpService(factory.Object, Config(@"C:\nonexistent\GeoLite2-City.mmdb"), NullLogger<GeoIpService>.Instance);

        var result = await svc.LookupAsync();

        result.PlayersUpdated.Should().Be(0);
        result.PlayersSkipped.Should().Be(0);
        factory.Verify(f => f.CreateDbContext(), Times.Never);
    }

    // ── Early-exit: no players needing lookup ────────────────────────────────

    [Fact]
    public async Task LookupAsync_AllPlayersAlreadyHaveFlag_ReturnsZeroCounts()
    {
        var dbFactory = EmptyDb();
        await using (var ctx = dbFactory.CreateDbContext())
        {
            ctx.Players.Add(new Player { PlayerId = 1, LastName = "Alice", Game = "dods", Flag = "US", LastAddress = "1.2.3.4" });
            ctx.Players.Add(new Player { PlayerId = 2, LastName = "Bob",   Game = "dods", Flag = "GB", LastAddress = "5.6.7.8" });
            await ctx.SaveChangesAsync();
        }

        var tmpFile = Path.GetTempFileName();
        try
        {
            var svc = new GeoIpService(dbFactory, Config(tmpFile), NullLogger<GeoIpService>.Instance);

            var result = await svc.LookupAsync();

            result.PlayersUpdated.Should().Be(0);
            result.PlayersSkipped.Should().Be(0);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task LookupAsync_PlayersHaveNoLastAddress_ReturnsZeroCounts()
    {
        var dbFactory = EmptyDb();
        await using (var ctx = dbFactory.CreateDbContext())
        {
            // flag is empty but lastAddress is also empty — Perl script excluded these
            ctx.Players.Add(new Player { PlayerId = 1, LastName = "Alice", Game = "dods", Flag = "", LastAddress = "" });
            await ctx.SaveChangesAsync();
        }

        var tmpFile = Path.GetTempFileName();
        try
        {
            var svc = new GeoIpService(dbFactory, Config(tmpFile), NullLogger<GeoIpService>.Instance);

            var result = await svc.LookupAsync();

            result.PlayersUpdated.Should().Be(0);
            result.PlayersSkipped.Should().Be(0);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
