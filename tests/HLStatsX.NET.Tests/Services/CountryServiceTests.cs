using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;

namespace HLStatsX.NET.Tests.Services;

public class CountryServiceTests
{
    private readonly Mock<ICountryRepository> _repoMock;
    private readonly CountryService _service;

    public CountryServiceTests()
    {
        _repoMock = new Mock<ICountryRepository>();
        _service  = new CountryService(_repoMock.Object);
    }

    [Fact]
    public async Task GetLeaderboardAsync_DelegatesToRepository()
    {
        var rows = new List<CountryLeaderboardRow>
        {
            new() { Flag = "us", Name = "United States", MemberCount = 10, AvgSkill = 1200 }
        };
        var paged = PagedResult<CountryLeaderboardRow>.Create(rows, 1, 1, 50);
        _repoMock.Setup(r => r.GetRankingsAsync("cstrike", 1, 50, "skill", true, 3, default))
                 .ReturnsAsync(paged);

        var result = await _service.GetLeaderboardAsync("cstrike", 1, 50);

        result.Items.Should().HaveCount(1);
        result.Items[0].Flag.Should().Be("us");
        _repoMock.Verify(r => r.GetRankingsAsync("cstrike", 1, 50, "skill", true, 3, default), Times.Once);
    }

    [Fact]
    public async Task GetTotalCountAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetTotalCountAsync("cstrike", default)).ReturnsAsync(42);

        var result = await _service.GetTotalCountAsync("cstrike");

        result.Should().Be(42);
        _repoMock.Verify(r => r.GetTotalCountAsync("cstrike", default), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetProfileAsync("xx", "cstrike", default))
                 .ReturnsAsync((CountryProfile?)null);

        var result = await _service.GetProfileAsync("xx", "cstrike");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfile_WhenFound()
    {
        var profile = new CountryProfile
        {
            Flag = "us", Name = "United States", MemberCount = 5, TotalKills = 3000, TotalDeaths = 1500, AvgSkill = 1100
        };
        _repoMock.Setup(r => r.GetProfileAsync("us", "cstrike", default)).ReturnsAsync(profile);

        var result = await _service.GetProfileAsync("us", "cstrike");

        result.Should().NotBeNull();
        result!.Name.Should().Be("United States");
        result.KillDeathRatio.Should().Be(2.0);
    }

    [Fact]
    public async Task GetMembersAsync_DelegatesToRepository()
    {
        var members = new List<CountryMember>
        {
            new() { PlayerId = 1, Name = "FragLord", Skill = 1500, Kills = 600, Deaths = 300 }
        };
        var paged = PagedResult<CountryMember>.Create(members, 1, 1, 50);
        _repoMock.Setup(r => r.GetMembersAsync("us", "cstrike", 1, 50, "skill", true, default))
                 .ReturnsAsync(paged);

        var result = await _service.GetMembersAsync("us", "cstrike", 1, 50);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("FragLord");
        _repoMock.Verify(r => r.GetMembersAsync("us", "cstrike", 1, 50, "skill", true, default), Times.Once);
    }

    [Fact]
    public async Task GetLeaderboardAsync_PassesMinMembers_ToRepository()
    {
        var paged = PagedResult<CountryLeaderboardRow>.Create([], 0, 1, 50);
        _repoMock.Setup(r => r.GetRankingsAsync("cstrike", 1, 50, "kills", false, 5, default))
                 .ReturnsAsync(paged);

        await _service.GetLeaderboardAsync("cstrike", 1, 50, "kills", false, 5);

        _repoMock.Verify(r => r.GetRankingsAsync("cstrike", 1, 50, "kills", false, 5, default), Times.Once);
    }
}
