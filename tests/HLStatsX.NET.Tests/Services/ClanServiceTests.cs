using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;
using Moq;

namespace HLStatsX.NET.Tests.Services;

public class ClanServiceTests
{
    private readonly Mock<IClanRepository> _repoMock;
    private readonly ClanService _service;

    public ClanServiceTests()
    {
        _repoMock = new Mock<IClanRepository>();
        _service = new ClanService(_repoMock.Object);
    }

    [Fact]
    public async Task GetClanAsync_ReturnsClan_WhenExists()
    {
        var clan = new Clan { ClanId = 1, Name = "FragForce", Tag = "[FF]", Game = "cstrike" };
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(clan);

        var result = await _service.GetClanAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("FragForce");
    }

    [Fact]
    public async Task GetClanAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Clan?)null);

        var result = await _service.GetClanAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsRankedClans()
    {
        var rows = new List<ClanLeaderboardRow>
        {
            new() { ClanId = 1, Name = "TopClan",    Tag = "[TC]", MemberCount = 5, AvgSkill = 1200 },
            new() { ClanId = 2, Name = "SecondClan", Tag = "[SC]", MemberCount = 3, AvgSkill = 1000 }
        };
        var paged = PagedResult<ClanLeaderboardRow>.Create(rows, 2, 1, 50);
        _repoMock.Setup(r => r.GetRankingsAsync("cstrike", 1, 50, "skill", true, 3, default)).ReturnsAsync(paged);

        var result = await _service.GetLeaderboardAsync("cstrike", 1, 50);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsMembersForClan()
    {
        var members = new List<Player>
        {
            new() { PlayerId = 1, LastName = "Leader", ClanId = 1 },
            new() { PlayerId = 2, LastName = "Member", ClanId = 1 }
        };
        _repoMock.Setup(r => r.GetMembersAsync(1, default)).ReturnsAsync(members);

        var result = await _service.GetMembersAsync(1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchClansAsync_ReturnsMatchingClans()
    {
        var matches = new List<Clan> { new() { ClanId = 3, Name = "FragClan", Tag = "[FC]" } };
        var paged = PagedResult<Clan>.Create(matches, 1, 1, 20);
        _repoMock.Setup(r => r.SearchAsync("Frag", "cstrike", 1, 20, default)).ReturnsAsync(paged);

        var result = await _service.SearchClansAsync("Frag", "cstrike", 1, 20);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Contain("Frag");
    }

    // ── GetTotalCountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTotalCountAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalCountAsync("cstrike", default)).ReturnsAsync(42);

        var result = await _service.GetTotalCountAsync("cstrike");

        result.Should().Be(42);
        _repoMock.Verify(r => r.GetTotalCountAsync("cstrike", default), Times.Once);
    }

    // ── GetClansByCountryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetClansByCountryAsync_DelegatesToRepository()
    {
        var clans = new List<Clan>
        {
            new() { ClanId = 1, Name = "GB Clan", Tag = "[GB]" },
            new() { ClanId = 2, Name = "UK Squad", Tag = "[UK]" }
        };
        _repoMock.Setup(r => r.GetByCountryAsync("GB", "cstrike", default)).ReturnsAsync(clans);

        var result = await _service.GetClansByCountryAsync("GB", "cstrike");

        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetByCountryAsync("GB", "cstrike", default), Times.Once);
    }

    // ── GetSummaryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ReturnsSummary_WhenFound()
    {
        var summary = new ClanSummaryStats(1000, 800, 200, 36000, 5, 10, 1250, 0.8);
        _repoMock.Setup(r => r.GetSummaryAsync(1, default)).ReturnsAsync(summary);

        var result = await _service.GetSummaryAsync(1);

        result.Should().NotBeNull();
        result!.TotalKills.Should().Be(1000);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetSummaryAsync(99, default)).ReturnsAsync((ClanSummaryStats?)null);

        var result = await _service.GetSummaryAsync(99);

        result.Should().BeNull();
    }

    // ── GetFavoriteServerAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteServerAsync_ReturnsFavorite_WhenFound()
    {
        var fav = new ClanFavoriteServer(5, "FragServer");
        _repoMock.Setup(r => r.GetFavoriteServerAsync(1, default)).ReturnsAsync(fav);

        var result = await _service.GetFavoriteServerAsync(1);

        result.Should().NotBeNull();
        result!.ServerName.Should().Be("FragServer");
    }

    [Fact]
    public async Task GetFavoriteServerAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetFavoriteServerAsync(99, default)).ReturnsAsync((ClanFavoriteServer?)null);

        var result = await _service.GetFavoriteServerAsync(99);

        result.Should().BeNull();
    }

    // ── GetFavoriteMapAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteMapAsync_ReturnsMapName_WhenFound()
    {
        _repoMock.Setup(r => r.GetFavoriteMapAsync(1, default)).ReturnsAsync("de_dust2");

        var result = await _service.GetFavoriteMapAsync(1);

        result.Should().Be("de_dust2");
    }

    [Fact]
    public async Task GetFavoriteMapAsync_ReturnsNull_WhenNoData()
    {
        _repoMock.Setup(r => r.GetFavoriteMapAsync(99, default)).ReturnsAsync((string?)null);

        var result = await _service.GetFavoriteMapAsync(99);

        result.Should().BeNull();
    }

    // ── GetFavoriteWeaponAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetFavoriteWeaponAsync_ReturnsFavorite_WhenFound()
    {
        var fav = new ClanFavoriteWeapon("ak47", "AK-47");
        _repoMock.Setup(r => r.GetFavoriteWeaponAsync(1, "cstrike", default)).ReturnsAsync(fav);

        var result = await _service.GetFavoriteWeaponAsync(1, "cstrike");

        result.Should().NotBeNull();
        result!.WeaponName.Should().Be("AK-47");
    }

    [Fact]
    public async Task GetFavoriteWeaponAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetFavoriteWeaponAsync(99, "cstrike", default)).ReturnsAsync((ClanFavoriteWeapon?)null);

        var result = await _service.GetFavoriteWeaponAsync(99, "cstrike");

        result.Should().BeNull();
    }

    // ── GetActionsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetActionsAsync_DelegatesToRepository()
    {
        var actions = new List<ClanActionRow> { new("plant", "Plant Bomb", 10, 50) };
        _repoMock.Setup(r => r.GetActionsAsync(1, default)).ReturnsAsync(actions);

        var result = await _service.GetActionsAsync(1);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("plant");
        _repoMock.Verify(r => r.GetActionsAsync(1, default), Times.Once);
    }

    // ── GetMemberLocationsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetMemberLocationsAsync_DelegatesToRepository()
    {
        var locs = new List<ClanMemberLocationRow>
        {
            new(1, "Player1", 100, 50, 51.5f, -0.1f, "London", "UK")
        };
        _repoMock.Setup(r => r.GetMemberLocationsAsync(1, default)).ReturnsAsync(locs);

        var result = await _service.GetMemberLocationsAsync(1);

        result.Should().HaveCount(1);
        result[0].City.Should().Be("London");
        _repoMock.Verify(r => r.GetMemberLocationsAsync(1, default), Times.Once);
    }
}
