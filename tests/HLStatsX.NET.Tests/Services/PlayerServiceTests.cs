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
        var row    = new BanListRow(10, "Hacker1", null, DateTime.UtcNow, 1000, 50, 100, 50, 20, 2.0, 0.2, 30.0);
        var paged  = PagedResult<BanListRow>.Create([row], 1, 1, 50);
        _repoMock.Setup(r => r.GetBannedAsync("cstrike", 1, 50, "ban_date", true, 0, default)).ReturnsAsync(paged);

        var result = await _service.GetBannedPlayersAsync("cstrike", 1, 50, "ban_date", true, 0);

        result.Items.Should().HaveCount(1);
        result.Items[0].PlayerName.Should().Be("Hacker1");
    }

    // ── UnbanPlayerAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UnbanPlayerAsync_ThrowsKeyNotFoundException_WhenPlayerNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Player?)null);

        await _service.Invoking(s => s.UnbanPlayerAsync(99))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetHistoryDatesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryDatesAsync_DelegatesToRepository()
    {
        var dates = new List<DateTime> { new(2025, 1, 1), new(2025, 1, 8) };
        _repoMock.Setup(r => r.GetHistoryDatesAsync("cstrike", 50, default)).ReturnsAsync(dates);

        var result = await _service.GetHistoryDatesAsync("cstrike");

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetHistoryDatesAsync("cstrike", 50, default), Times.Once);
    }

    // ── GetPlayerHistoryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerHistoryAsync_DelegatesToRepository()
    {
        var history = new List<PlayerHistory>
        {
            new() { PlayerId = 1, Skill = 1500, Kills = 100 },
            new() { PlayerId = 1, Skill = 1480, Kills = 95 },
        };
        _repoMock.Setup(r => r.GetHistoryAsync(1, 30, default)).ReturnsAsync(history);

        var result = await _service.GetPlayerHistoryAsync(1);

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetHistoryAsync(1, 30, default), Times.Once);
    }

    // ── GetTotalKillsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTotalKillsAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalKillsAsync("cstrike", default)).ReturnsAsync(999_999L);

        var result = await _service.GetTotalKillsAsync("cstrike");

        result.Should().Be(999_999L);
        _repoMock.Verify(r => r.GetTotalKillsAsync("cstrike", default), Times.Once);
    }

    // ── GetTotalCountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTotalCountAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalCountAsync("cstrike", default)).ReturnsAsync(500);

        var result = await _service.GetTotalCountAsync("cstrike");

        result.Should().Be(500);
        _repoMock.Verify(r => r.GetTotalCountAsync("cstrike", default), Times.Once);
    }

    // ── GetPeriodLeaderboardAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPeriodLeaderboardAsync_DelegatesToRepository()
    {
        var from = new DateTime(2025, 1, 1);
        var to   = new DateTime(2025, 1, 8);
        var rows = new List<PlayerLeaderboardRow>
        {
            new() { PlayerId = 1, LastName = "FragMaster", Points = 2000, Kills = 500 }
        };
        var paged = PagedResult<PlayerLeaderboardRow>.Create(rows, 1, 1, 50);
        _repoMock.Setup(r => r.GetHistoryRankingsAsync("cstrike", from, to, 1, 50, "skill", true, 1, default))
                 .ReturnsAsync(paged);

        var result = await _service.GetPeriodLeaderboardAsync("cstrike", from, to, 1, 50, "skill", true);

        result.Items.Should().HaveCount(1);
        result.Items[0].LastName.Should().Be("FragMaster");
        _repoMock.Verify(r => r.GetHistoryRankingsAsync("cstrike", from, to, 1, 50, "skill", true, 1, default), Times.Once);
    }

    // ── GetPlayerRankAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerRankAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetRankAsync(1, "cstrike", default)).ReturnsAsync(42);

        var result = await _service.GetPlayerRankAsync(1, "cstrike");

        result.Should().Be(42);
        _repoMock.Verify(r => r.GetRankAsync(1, "cstrike", default), Times.Once);
    }

    // ── GetTrendDataAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTrendDataAsync_DelegatesToRepository()
    {
        var trend = new List<TrendPoint>
        {
            new(new DateTime(2025, 5, 1), 1500, 20),
            new(new DateTime(2025, 5, 2), 1520, 20),
        };
        _repoMock.Setup(r => r.GetTrendAsync(5, 30, default)).ReturnsAsync(trend);

        var result = await _service.GetTrendDataAsync(5, 30);

        result.Should().HaveCount(2);
        result[0].Skill.Should().Be(1500);
        _repoMock.Verify(r => r.GetTrendAsync(5, 30, default), Times.Once);
    }

    // ── GetRealStatsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealStatsAsync_DelegatesToStatsRepository()
    {
        var stats = new RealStats(200, 100, 50, 10, 2.0, 0.5);
        _statsMock.Setup(r => r.GetRealStatsAsync(1, default)).ReturnsAsync(stats);

        var result = await _service.GetRealStatsAsync(1);

        result.Should().Be(stats);
        _statsMock.Verify(r => r.GetRealStatsAsync(1, default), Times.Once);
    }

    // ── GetAveragePingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAveragePingAsync_ReturnsNull_WhenNoData()
    {
        _statsMock.Setup(r => r.GetAveragePingAsync(1, default)).ReturnsAsync((PingStats?)null);

        var result = await _service.GetAveragePingAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAveragePingAsync_ReturnsPingStats_WhenAvailable()
    {
        var ping = new PingStats(45, 5);
        _statsMock.Setup(r => r.GetAveragePingAsync(1, default)).ReturnsAsync(ping);

        var result = await _service.GetAveragePingAsync(1);

        result.Should().Be(ping);
    }

    // ── GetLastConnectAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetLastConnectAsync_DelegatesToStatsRepository()
    {
        var ts = new DateTime(2025, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        _statsMock.Setup(r => r.GetLastConnectAsync(1, default)).ReturnsAsync(ts);

        var result = await _service.GetLastConnectAsync(1);

        result.Should().Be(ts);
        _statsMock.Verify(r => r.GetLastConnectAsync(1, default), Times.Once);
    }

    // ── GetFavoriteServerAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteServerAsync_DelegatesToStatsRepository()
    {
        var fav = new FavoriteServer(3, "FragArena");
        _statsMock.Setup(r => r.GetFavoriteServerAsync(1, default)).ReturnsAsync(fav);

        var result = await _service.GetFavoriteServerAsync(1);

        result.Should().Be(fav);
        _statsMock.Verify(r => r.GetFavoriteServerAsync(1, default), Times.Once);
    }

    // ── GetFavoriteMapAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteMapAsync_DelegatesToStatsRepository()
    {
        _statsMock.Setup(r => r.GetFavoriteMapAsync(1, default)).ReturnsAsync("de_dust2");

        var result = await _service.GetFavoriteMapAsync(1);

        result.Should().Be("de_dust2");
        _statsMock.Verify(r => r.GetFavoriteMapAsync(1, default), Times.Once);
    }

    // ── GetFavoriteWeaponAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteWeaponAsync_DelegatesToStatsRepository()
    {
        var weapon = new FavoriteWeapon("awp", "AWP");
        _statsMock.Setup(r => r.GetFavoriteWeaponAsync(1, default)).ReturnsAsync(weapon);

        var result = await _service.GetFavoriteWeaponAsync(1);

        result.Should().Be(weapon);
        _statsMock.Verify(r => r.GetFavoriteWeaponAsync(1, default), Times.Once);
    }

    // ── GetNextRankAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetNextRankAsync_ReturnsNull_AtTopRank()
    {
        _statsMock.Setup(r => r.GetNextRankAsync("cstrike", 99999, default)).ReturnsAsync((Rank?)null);

        var result = await _service.GetNextRankAsync("cstrike", 99999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextRankAsync_ReturnsNextRank_WhenAvailable()
    {
        var rank = new Rank { RankId = 5, RankName = "Veteran", MinKills = 500 };
        _statsMock.Setup(r => r.GetNextRankAsync("cstrike", 200, default)).ReturnsAsync(rank);

        var result = await _service.GetNextRankAsync("cstrike", 200);

        result.Should().Be(rank);
    }

    // ── GetGlobalAwardsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetGlobalAwardsAsync_DelegatesToStatsRepository()
    {
        var awards = new List<GlobalAwardRow>
        {
            new("W", "awp", "AWP Most Kills")
        };
        _statsMock.Setup(r => r.GetGlobalAwardsAsync(1, "cstrike", default)).ReturnsAsync(awards);

        var result = await _service.GetGlobalAwardsAsync(1, "cstrike");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("AWP Most Kills");
        _statsMock.Verify(r => r.GetGlobalAwardsAsync(1, "cstrike", default), Times.Once);
    }

    // ── GetDeleteDaysAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDeleteDaysAsync_DelegatesToStatsRepository()
    {
        _statsMock.Setup(r => r.GetDeleteDaysAsync(default)).ReturnsAsync(180);

        var result = await _service.GetDeleteDaysAsync();

        result.Should().Be(180);
        _statsMock.Verify(r => r.GetDeleteDaysAsync(default), Times.Once);
    }

    // ── GetKillStatsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetKillStatsAsync_DelegatesToStatsRepository()
    {
        var rows = new List<KillStatRow>
        {
            new(2, "Victim1", 10, 5, 3),
        };
        _statsMock.Setup(r => r.GetKillStatsAsync(1, 0, default)).ReturnsAsync(rows);

        var result = await _service.GetKillStatsAsync(1, 0);

        result.Should().HaveCount(1);
        result[0].VictimName.Should().Be("Victim1");
        _statsMock.Verify(r => r.GetKillStatsAsync(1, 0, default), Times.Once);
    }

    // ── GetRibbonsWithStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetRibbonsWithStatusAsync_DelegatesToStatsRepository()
    {
        var ribbons = new List<RibbonDisplay>
        {
            new(1, "First Blood", null, true)
        };
        _statsMock.Setup(r => r.GetRibbonsWithStatusAsync(1, "cstrike", default)).ReturnsAsync(ribbons);

        var result = await _service.GetRibbonsWithStatusAsync(1, "cstrike");

        result.Should().HaveCount(1);
        result[0].RibbonName.Should().Be("First Blood");
        _statsMock.Verify(r => r.GetRibbonsWithStatusAsync(1, "cstrike", default), Times.Once);
    }

    // ── GetMapPerformanceAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetMapPerformanceAsync_DelegatesToStatsRepository()
    {
        var rows = new List<MapStatRow>
        {
            new("de_dust2", 100, 60, 20)
        };
        _statsMock.Setup(r => r.GetMapPerformanceAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetMapPerformanceAsync(1);

        result.Should().HaveCount(1);
        result[0].Map.Should().Be("de_dust2");
        _statsMock.Verify(r => r.GetMapPerformanceAsync(1, default), Times.Once);
    }

    // ── GetWeaponStatsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetWeaponStatsAsync_DelegatesToStatsRepository()
    {
        var rows = new List<WeaponStatRow>
        {
            new("awp", "AWP", 1.0f, 50, 30)
        };
        _statsMock.Setup(r => r.GetWeaponStatsAsync(1, "cstrike", default)).ReturnsAsync(rows);

        var result = await _service.GetWeaponStatsAsync(1, "cstrike");

        result.Should().HaveCount(1);
        result[0].WeaponName.Should().Be("AWP");
        _statsMock.Verify(r => r.GetWeaponStatsAsync(1, "cstrike", default), Times.Once);
    }

    // ── GetTeamSelectionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamSelectionAsync_DelegatesToStatsRepository()
    {
        var rows = new List<TeamStatRow>
        {
            new("ct", "Counter-Terrorists", 80, 100)
        };
        _statsMock.Setup(r => r.GetTeamSelectionAsync(1, "cstrike", default)).ReturnsAsync(rows);

        var result = await _service.GetTeamSelectionAsync(1, "cstrike");

        result.Should().HaveCount(1);
        result[0].TeamName.Should().Be("Counter-Terrorists");
        _statsMock.Verify(r => r.GetTeamSelectionAsync(1, "cstrike", default), Times.Once);
    }

    // ── GetPlayerActionsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerActionsAsync_DelegatesToStatsRepository()
    {
        var rows = new List<ActionStatRow>
        {
            new("Bomb Plant", 15, 75)
        };
        _statsMock.Setup(r => r.GetPlayerActionsAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetPlayerActionsAsync(1);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Bomb Plant");
        _statsMock.Verify(r => r.GetPlayerActionsAsync(1, default), Times.Once);
    }

    // ── GetPlayerActionVictimsAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetPlayerActionVictimsAsync_DelegatesToStatsRepository()
    {
        var rows = new List<ActionStatRow>
        {
            new("Bomb Defuse", 3, 15)
        };
        _statsMock.Setup(r => r.GetPlayerActionVictimsAsync(1, default)).ReturnsAsync(rows);

        var result = await _service.GetPlayerActionVictimsAsync(1);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Bomb Defuse");
        _statsMock.Verify(r => r.GetPlayerActionVictimsAsync(1, default), Times.Once);
    }
}
