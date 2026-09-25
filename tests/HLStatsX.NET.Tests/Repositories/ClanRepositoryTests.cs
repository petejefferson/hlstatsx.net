using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using HLStatsX.NET.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Tests.Repositories;

public class ClanRepositoryTests : IDisposable
{
    private readonly HLStatsDbContext _db;
    private readonly ClanRepository _repo;

    public ClanRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HLStatsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db   = new TestDbContext(options);
        _repo = new ClanRepository(new TestDbContextFactory(options));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsClan_WhenExists()
    {
        _db.Clans.Add(new Clan { ClanId = 1, Name = "TestClan", Tag = "[TC]", Game = "cstrike" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("TestClan");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repo.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsPlayersInClan()
    {
        _db.Clans.Add(new Clan { ClanId = 1, Name = "Clan1", Tag = "[C1]", Game = "cstrike" });
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "P1", Game = "cstrike", ClanId = 1, Skill = 1000, Kills = 50 },
            new Player { PlayerId = 2, LastName = "P2", Game = "cstrike", ClanId = 1, Skill = 1200, Kills = 80 },
            new Player { PlayerId = 3, LastName = "P3", Game = "cstrike", ClanId = null, Skill = 1500, Kills = 100 }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetMembersAsync(1);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.ClanId.Should().Be(1));
    }

    [Fact]
    public async Task SearchAsync_FindsClansByName()
    {
        _db.Clans.AddRange(
            new Clan { ClanId = 1, Name = "FragForce", Tag = "[FF]", Game = "cstrike" },
            new Clan { ClanId = 2, Name = "SniperSquad", Tag = "[SS]", Game = "cstrike" }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync("Frag", "cstrike", 1, 20);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("FragForce");
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsOnlyVisibleClans()
    {
        _db.Clans.AddRange(
            new Clan { ClanId = 1, Name = "Visible", Tag = "[V]", Game = "cstrike", IsHidden = false },
            new Clan { ClanId = 2, Name = "Hidden", Tag = "[H]", Game = "cstrike", IsHidden = true }
        );
        await _db.SaveChangesAsync();

        var count = await _repo.GetTotalCountAsync("cstrike");

        count.Should().Be(1);
    }

    public void Dispose() => _db.Dispose();
}
