using FluentAssertions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;
using Moq;

namespace HLStatsX.NET.Tests.Services;

public class SearchServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IClanRepository> _clanRepoMock;
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _clanRepoMock = new Mock<IClanRepository>();
        _service = new SearchService(_playerRepoMock.Object, _clanRepoMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ReturnsCombinedResults()
    {
        var players = new List<PlayerSearchResult>
        {
            new(1, "FragMaster", null, null, "cstrike")
        };
        var clans = new List<Clan> { new() { ClanId = 1, Name = "FragClan" } };

        _playerRepoMock
            .Setup(r => r.SearchAsync("Frag", "cstrike", 1, 20, default))
            .ReturnsAsync(PagedResult<PlayerSearchResult>.Create(players, 1, 1, 20));

        _clanRepoMock
            .Setup(r => r.SearchAsync("Frag", "cstrike", 1, 20, default))
            .ReturnsAsync(PagedResult<Clan>.Create(clans, 1, 1, 20));

        var result = await _service.SearchAsync("Frag", "cstrike", 1, 20);

        result.Players.Should().HaveCount(1);
        result.Clans.Should().HaveCount(1);
        result.TotalPlayers.Should().Be(1);
        result.TotalClans.Should().Be(1);
        result.Query.Should().Be("Frag");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatches()
    {
        _playerRepoMock
            .Setup(r => r.SearchAsync("xyz", "cstrike", 1, 20, default))
            .ReturnsAsync(PagedResult<PlayerSearchResult>.Create(Array.Empty<PlayerSearchResult>(), 0, 1, 20));

        _clanRepoMock
            .Setup(r => r.SearchAsync("xyz", "cstrike", 1, 20, default))
            .ReturnsAsync(PagedResult<Clan>.Create(Array.Empty<Clan>(), 0, 1, 20));

        var result = await _service.SearchAsync("xyz", "cstrike");

        result.Players.Should().BeEmpty();
        result.Clans.Should().BeEmpty();
        result.TotalPlayers.Should().Be(0);
        result.TotalClans.Should().Be(0);
    }
}
