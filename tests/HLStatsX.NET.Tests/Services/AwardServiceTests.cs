using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Tests.Services;

public class AwardServiceTests
{
    private readonly Mock<IAwardRepository> _repoMock;
    private readonly IMemoryCache _cache;
    private readonly AwardService _service;

    public AwardServiceTests()
    {
        _repoMock = new Mock<IAwardRepository>();
        _cache    = new MemoryCache(new MemoryCacheOptions());
        _service  = new AwardService(_repoMock.Object, _cache);
    }

    // ── GetRanksAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRanksAsync_ReturnsRanksFromRepository_OnFirstCall()
    {
        var ranks = new List<Rank>
        {
            new() { RankId = 1, RankName = "Private",  MinKills = 0   },
            new() { RankId = 2, RankName = "Corporal", MinKills = 100 },
        };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        var result = await _service.GetRanksAsync("cstrike");

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetRanksAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetRanksAsync_ReturnsCachedValue_OnSecondCall()
    {
        var ranks = new List<Rank> { new() { RankId = 1, RankName = "Private", MinKills = 0 } };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        await _service.GetRanksAsync("cstrike");
        await _service.GetRanksAsync("cstrike");

        // Repository must only be called once — second hit is served from cache
        _repoMock.Verify(r => r.GetRanksAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetRanksAsync_CachesPerGame()
    {
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync([new() { RankId = 1, MinKills = 0 }]);
        _repoMock.Setup(r => r.GetRanksAsync("dods",    default)).ReturnsAsync([new() { RankId = 2, MinKills = 0 }]);

        await _service.GetRanksAsync("cstrike");
        await _service.GetRanksAsync("dods");

        _repoMock.Verify(r => r.GetRanksAsync("cstrike", default), Times.Once);
        _repoMock.Verify(r => r.GetRanksAsync("dods",    default), Times.Once);
    }

    // ── GetRibbonsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetRibbonsAsync_ReturnsCachedValue_OnSecondCall()
    {
        var ribbons = new List<Ribbon> { new() { RibbonId = 1, RibbonName = "First Blood" } };
        _repoMock.Setup(r => r.GetRibbonsAsync("cstrike", default)).ReturnsAsync(ribbons);

        await _service.GetRibbonsAsync("cstrike");
        await _service.GetRibbonsAsync("cstrike");

        _repoMock.Verify(r => r.GetRibbonsAsync("cstrike", default), Times.Once);
    }

    // ── GetRankForPlayerAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRankForPlayerAsync_ReturnsHighestQualifyingRank()
    {
        var ranks = new List<Rank>
        {
            new() { RankId = 1, RankName = "Private",  MinKills = 0   },
            new() { RankId = 2, RankName = "Corporal", MinKills = 100 },
            new() { RankId = 3, RankName = "Sergeant", MinKills = 500 },
        };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        // 250 kills qualifies for Private (0) and Corporal (100) but not Sergeant (500)
        var result = await _service.GetRankForPlayerAsync(1, "cstrike", 250);

        result.Should().NotBeNull();
        result!.RankName.Should().Be("Corporal");
    }

    [Fact]
    public async Task GetRankForPlayerAsync_ReturnsNull_WhenNoThresholdMet()
    {
        var ranks = new List<Rank>
        {
            new() { RankId = 1, RankName = "Private", MinKills = 50 },
        };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        // 10 kills does not meet the 50-kill threshold
        var result = await _service.GetRankForPlayerAsync(1, "cstrike", 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRankForPlayerAsync_ReturnsHighestWhenKillsExceedAll()
    {
        var ranks = new List<Rank>
        {
            new() { RankId = 1, RankName = "Private",  MinKills = 0    },
            new() { RankId = 2, RankName = "General",  MinKills = 5000 },
        };
        _repoMock.Setup(r => r.GetRanksAsync("cstrike", default)).ReturnsAsync(ranks);

        var result = await _service.GetRankForPlayerAsync(1, "cstrike", 9999);

        result!.RankName.Should().Be("General");
    }

    // ── GetAwardsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAwardsAsync_DelegatesToRepository()
    {
        var awards = new List<Award> { new() { AwardId = 1, Name = "Best Fragger" } };
        _repoMock.Setup(r => r.GetAllAsync("cstrike", default)).ReturnsAsync(awards);

        var result = await _service.GetAwardsAsync("cstrike");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Best Fragger");
        _repoMock.Verify(r => r.GetAllAsync("cstrike", default), Times.Once);
    }

    // ── GetDailyAwardsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDailyAwardsAsync_DelegatesToRepository()
    {
        var awards = new List<Award> { new() { AwardId = 2, Name = "Daily Fragger" } };
        _repoMock.Setup(r => r.GetDailyAwardsAsync("cstrike", default)).ReturnsAsync(awards);

        var result = await _service.GetDailyAwardsAsync("cstrike");

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetDailyAwardsAsync("cstrike", default), Times.Once);
    }

    // ── GetRibbonAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRibbonAsync_ReturnsRibbon_WhenFound()
    {
        var ribbon = new Ribbon { RibbonId = 3, RibbonName = "First Blood" };
        _repoMock.Setup(r => r.GetRibbonByIdAsync(3, default)).ReturnsAsync(ribbon);

        var result = await _service.GetRibbonAsync(3);

        result.Should().NotBeNull();
        result!.RibbonName.Should().Be("First Blood");
    }

    [Fact]
    public async Task GetRibbonAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetRibbonByIdAsync(99, default)).ReturnsAsync((Ribbon?)null);

        var result = await _service.GetRibbonAsync(99);

        result.Should().BeNull();
    }

    // ── GetRanksWithCountsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRanksWithCountsAsync_DelegatesToRepository()
    {
        var rank = new Rank { RankId = 1, RankName = "Private", MinKills = 0 };
        var rows = new List<RankRow> { new(rank, 42) };
        _repoMock.Setup(r => r.GetRanksWithCountsAsync("cstrike", default)).ReturnsAsync(rows);

        var result = await _service.GetRanksWithCountsAsync("cstrike");

        result.Should().HaveCount(1);
        result[0].PlayerCount.Should().Be(42);
        _repoMock.Verify(r => r.GetRanksWithCountsAsync("cstrike", default), Times.Once);
    }

    // ── GetRibbonsWithCountsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetRibbonsWithCountsAsync_DelegatesToRepository()
    {
        var ribbon = new Ribbon { RibbonId = 1, RibbonName = "Ace" };
        var rows   = new List<RibbonRow> { new(ribbon, 10, "Best Fragger") };
        _repoMock.Setup(r => r.GetRibbonsWithCountsAsync("cstrike", default)).ReturnsAsync(rows);

        var result = await _service.GetRibbonsWithCountsAsync("cstrike");

        result.Should().HaveCount(1);
        result[0].AchievedCount.Should().Be(10);
        _repoMock.Verify(r => r.GetRibbonsWithCountsAsync("cstrike", default), Times.Once);
    }

    // ── GetAwardByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAwardByIdAsync_ReturnsAward_WhenFound()
    {
        var award = new Award { AwardId = 5, Name = "Sniper Elite" };
        _repoMock.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(award);

        var result = await _service.GetAwardByIdAsync(5);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Sniper Elite");
    }

    [Fact]
    public async Task GetAwardByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Award?)null);

        var result = await _service.GetAwardByIdAsync(99);

        result.Should().BeNull();
    }

    // ── GetDailyAwardHistoryAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetDailyAwardHistoryAsync_DelegatesToRepository()
    {
        var rows  = new List<DailyAwardHistoryRow> { new(1, DateTime.UtcNow, "FragMaster", "gb", 3) };
        var paged = PagedResult<DailyAwardHistoryRow>.Create(rows, 1, 1, 20);
        _repoMock.Setup(r => r.GetDailyAwardHistoryAsync(5, 1, 20, "awardTime", true, default)).ReturnsAsync(paged);

        var result = await _service.GetDailyAwardHistoryAsync(5, 1, 20, "awardTime", true);

        result.Items.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetDailyAwardHistoryAsync(5, 1, 20, "awardTime", true, default), Times.Once);
    }

    // ── GetRankByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRankByIdAsync_ReturnsRank_WhenFound()
    {
        var rank = new Rank { RankId = 2, RankName = "Sergeant", MinKills = 500 };
        _repoMock.Setup(r => r.GetRankByIdAsync(2, default)).ReturnsAsync(rank);

        var result = await _service.GetRankByIdAsync(2);

        result.Should().NotBeNull();
        result!.RankName.Should().Be("Sergeant");
    }

    [Fact]
    public async Task GetRankByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetRankByIdAsync(99, default)).ReturnsAsync((Rank?)null);

        var result = await _service.GetRankByIdAsync(99);

        result.Should().BeNull();
    }

    // ── GetRankDetailAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetRankDetailAsync_DelegatesToRepository()
    {
        var rows  = new List<RankPlayerRow> { new(1, "FragMaster", "gb", 500, 2000) };
        var paged = PagedResult<RankPlayerRow>.Create(rows, 1, 1, 20);
        _repoMock.Setup(r => r.GetRankDetailAsync(2, "cstrike", 1, 20, "kills", true, default)).ReturnsAsync(paged);

        var result = await _service.GetRankDetailAsync(2, "cstrike", 1, 20, "kills", true);

        result.Items.Should().HaveCount(1);
        result.Items[0].PlayerName.Should().Be("FragMaster");
        _repoMock.Verify(r => r.GetRankDetailAsync(2, "cstrike", 1, 20, "kills", true, default), Times.Once);
    }
}
