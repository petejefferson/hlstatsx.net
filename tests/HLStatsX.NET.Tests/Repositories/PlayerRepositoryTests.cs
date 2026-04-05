using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Infrastructure.Data;
using HLStatsX.NET.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Tests.Repositories;

public class PlayerRepositoryTests : IDisposable
{
    private readonly HLStatsDbContext _db;
    private readonly PlayerRepository _repo;

    public PlayerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HLStatsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _repo = new PlayerRepository(_db);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPlayer_WhenExists()
    {
        _db.Players.Add(new Player { PlayerId = 1, LastName = "TestPlayer", Game = "cstrike", Skill = 1000, Kills = 50 });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.LastName.Should().Be("TestPlayer");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repo.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRankingsAsync_ReturnsSortedBySkillDescending()
    {
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "Low", Game = "cstrike", Skill = 800, Kills = 10 },
            new Player { PlayerId = 2, LastName = "High", Game = "cstrike", Skill = 2000, Kills = 200 },
            new Player { PlayerId = 3, LastName = "Mid", Game = "cstrike", Skill = 1200, Kills = 80 }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetRankingsAsync("cstrike", 1, 50);

        result.Items[0].LastName.Should().Be("High");
        result.Items[1].LastName.Should().Be("Mid");
        result.Items[2].LastName.Should().Be("Low");
    }

    [Fact]
    public async Task GetRankingsAsync_ExcludesHiddenPlayers()
    {
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "Visible", Game = "cstrike", Skill = 1000, Kills = 50, HideRanking = 0 },
            new Player { PlayerId = 2, LastName = "Hidden", Game = "cstrike", Skill = 2000, Kills = 200, HideRanking = 1 }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetRankingsAsync("cstrike", 1, 50);

        result.Items.Should().HaveCount(1);
        result.Items[0].LastName.Should().Be("Visible");
    }

    [Fact]
    public async Task GetRankingsAsync_ExcludesPlayersWithZeroKills()
    {
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "Active", Game = "cstrike", Skill = 1000, Kills = 50 },
            new Player { PlayerId = 2, LastName = "NoKills", Game = "cstrike", Skill = 1200, Kills = 0 }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetRankingsAsync("cstrike", 1, 50);

        result.Items.Should().HaveCount(1);
        result.Items[0].LastName.Should().Be("Active");
    }

    [Fact]
    public async Task GetRankAsync_ReturnsCorrectRank()
    {
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "First", Game = "cstrike", Skill = 3000, Kills = 100 },
            new Player { PlayerId = 2, LastName = "Second", Game = "cstrike", Skill = 2000, Kills = 80 },
            new Player { PlayerId = 3, LastName = "Third", Game = "cstrike", Skill = 1000, Kills = 30 }
        );
        await _db.SaveChangesAsync();

        var rank = await _repo.GetRankAsync(3, "cstrike");

        rank.Should().Be(3);
    }

    [Fact]
    public async Task GetBannedAsync_ReturnsOnlyBannedPlayers()
    {
        _db.Players.AddRange(
            new Player { PlayerId = 1, LastName = "Clean", Game = "cstrike", HideRanking = 0 },
            new Player { PlayerId = 2, LastName = "Cheater", Game = "cstrike", HideRanking = 1 }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetBannedAsync("cstrike");

        result.Should().HaveCount(1);
        result[0].LastName.Should().Be("Cheater");
    }

    [Fact]
    public async Task GetRankingsAsync_RespectsPageSizeAndPage()
    {
        for (int i = 1; i <= 10; i++)
        {
            _db.Players.Add(new Player { PlayerId = i, LastName = $"Player{i}", Game = "cstrike", Skill = i * 100, Kills = i * 10 });
        }
        await _db.SaveChangesAsync();

        var page1 = await _repo.GetRankingsAsync("cstrike", 1, 3);
        var page2 = await _repo.GetRankingsAsync("cstrike", 2, 3);

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(3);
        page1.TotalCount.Should().Be(10);
        page1.TotalPages.Should().Be(4);
    }

    public void Dispose() => _db.Dispose();
}
