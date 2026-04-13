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
}
