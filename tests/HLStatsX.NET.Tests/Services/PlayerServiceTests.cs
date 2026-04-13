using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;

namespace HLStatsX.NET.Tests.Services;

public class PlayerServiceTests
{
    private readonly Mock<IPlayerRepository> _repoMock;
    private readonly Mock<IPlayerStatsRepository> _statsMock;
    private readonly PlayerService _service;

    public PlayerServiceTests()
    {
        _repoMock  = new Mock<IPlayerRepository>();
        _statsMock = new Mock<IPlayerStatsRepository>();
        _service   = new PlayerService(_repoMock.Object, _statsMock.Object);
    }

    [Fact]
    public async Task GetPlayerAsync_ReturnsPlayer_WhenPlayerExists()
    {
        var player = new Player { PlayerId = 1, LastName = "Fraglord", Game = "cstrike", Skill = 1500 };
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(player);

        var result = await _service.GetPlayerAsync(1);

        result.Should().NotBeNull();
        result!.PlayerId.Should().Be(1);
        result.LastName.Should().Be("Fraglord");
    }

    [Fact]
    public async Task GetPlayerAsync_ReturnsNull_WhenPlayerNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Player?)null);

        var result = await _service.GetPlayerAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsPagedResult()
    {
        var players = new List<Player>
        {
            new() { PlayerId = 1, LastName = "Alpha", Game = "cstrike", Skill = 2000, Kills = 500 },
            new() { PlayerId = 2, LastName = "Bravo", Game = "cstrike", Skill = 1800, Kills = 400 }
        };
        var expected = PagedResult<Player>.Create(players, 2, 1, 50);
        _repoMock.Setup(r => r.GetRankingsAsync("cstrike", 1, 50, "skill", true, 1, default)).ReturnsAsync(expected);

        var result = await _service.GetLeaderboardAsync("cstrike", 1, 50);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Skill.Should().BeGreaterThan(result.Items[1].Skill);
    }

    [Fact]
    public async Task BanPlayerAsync_SetsHideRanking()
    {
        var player = new Player { PlayerId = 5, LastName = "Cheater", Game = "cstrike", HideRanking = 0 };
        _repoMock.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(player);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Player>(), default)).Returns(Task.CompletedTask);

        await _service.BanPlayerAsync(5, "Cheating");

        player.HideRanking.Should().Be(1);
        _repoMock.Verify(r => r.UpdateAsync(player, default), Times.Once);
    }

    [Fact]
    public async Task BanPlayerAsync_ThrowsKeyNotFoundException_WhenPlayerNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Player?)null);

        await _service.Invoking(s => s.BanPlayerAsync(99, "reason"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UnbanPlayerAsync_ClearsHideRanking()
    {
        var player = new Player { PlayerId = 3, HideRanking = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(3, default)).ReturnsAsync(player);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Player>(), default)).Returns(Task.CompletedTask);

        await _service.UnbanPlayerAsync(3);

        player.HideRanking.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerAliasesAsync_ReturnsDelegatedToRepository()
    {
        var aliases = new List<PlayerName> { new() { Name = "OldName", PlayerId = 1 } };
        _repoMock.Setup(r => r.GetAliasesAsync(1, default)).ReturnsAsync(aliases);

        var result = await _service.GetPlayerAliasesAsync(1);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("OldName");
    }

    [Fact]
    public async Task GetBannedPlayersAsync_ReturnsBannedPlayers()
    {
        var banned = new List<Player>
        {
            new() { PlayerId = 10, LastName = "Hacker1", HideRanking = 1, Game = "cstrike" }
        };
        _repoMock.Setup(r => r.GetBannedAsync("cstrike", default)).ReturnsAsync(banned);

        var result = await _service.GetBannedPlayersAsync("cstrike");

        result.Should().HaveCount(1);
        result[0].IsHidden.Should().BeTrue();
    }
}
