using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class CountriesControllerTests
{
    private readonly Mock<ICountryService> _countriesMock;
    private readonly IConfiguration _config;
    private readonly CountriesController _controller;

    public CountriesControllerTests()
    {
        _countriesMock = new Mock<ICountryService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new CountriesController(_countriesMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithCountryLeaderboard()
    {
        var rows = new List<CountryLeaderboardRow>
        {
            new() { Flag = "us", Name = "United States", MemberCount = 10, TotalKills = 5000, AvgSkill = 1200 }
        };
        var paged = PagedResult<CountryLeaderboardRow>.Create(rows, 1, 1, 50);

        _countriesMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "members", true, 3, default))
                      .ReturnsAsync(paged);
        _countriesMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(1);

        var result = await _controller.Index(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<CountryLeaderboardViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Countries.Items.Should().HaveCount(1);
        model.TotalCountries.Should().Be(1);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        _countriesMock.Setup(s => s.GetLeaderboardAsync("dods", 1, 50, "members", true, 3, default))
                      .ReturnsAsync(PagedResult<CountryLeaderboardRow>.Create([], 0, 1, 50));
        _countriesMock.Setup(s => s.GetTotalCountAsync("dods", default)).ReturnsAsync(0);

        var result = await _controller.Index("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<CountryLeaderboardViewModel>()
              .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Index_PassesMinMembers_ToService()
    {
        _countriesMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "members", true, 5, default))
                      .ReturnsAsync(PagedResult<CountryLeaderboardRow>.Create([], 0, 1, 50));
        _countriesMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(0);

        await _controller.Index(null, minMembers: 5);

        _countriesMock.Verify(
            s => s.GetLeaderboardAsync("cstrike", 1, 50, "members", true, 5, default), Times.Once);
    }

    [Fact]
    public async Task Index_ExposesMinMembers_InViewModel()
    {
        _countriesMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "members", true, 7, default))
                      .ReturnsAsync(PagedResult<CountryLeaderboardRow>.Create([], 0, 1, 50));
        _countriesMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(0);

        var result = await _controller.Index(null, minMembers: 7);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<CountryLeaderboardViewModel>()
              .Which.MinMembers.Should().Be(7);
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        _countriesMock.Setup(s => s.GetProfileAsync("xx", "cstrike", default))
                      .ReturnsAsync((CountryProfile?)null);

        var result = await _controller.Profile("xx", "cstrike");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Profile_ReturnsView_WithProfileAndMembers()
    {
        var profile = new CountryProfile
        {
            Flag = "us", Name = "United States",
            MemberCount = 5, TotalKills = 3000, TotalDeaths = 2000, AvgSkill = 1100
        };
        var members = PagedResult<CountryMember>.Create(
            [new() { PlayerId = 1, Name = "FragLord", Skill = 1500, Kills = 600, Deaths = 300 }],
            1, 1, 50);

        _countriesMock.Setup(s => s.GetProfileAsync("us", "cstrike", default)).ReturnsAsync(profile);
        _countriesMock.Setup(s => s.GetMembersAsync("us", "cstrike", 1, 50, "skill", true, default))
                      .ReturnsAsync(members);

        var result = await _controller.Profile("us", "cstrike");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<CountryProfileViewModel>().Subject;
        model.Profile.Name.Should().Be("United States");
        model.Members.Items.Should().HaveCount(1);
        model.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Profile_UsesDefaultGame_WhenGameIsNull()
    {
        var profile = new CountryProfile { Flag = "us", Name = "United States" };
        _countriesMock.Setup(s => s.GetProfileAsync("us", "cstrike", default)).ReturnsAsync(profile);
        _countriesMock.Setup(s => s.GetMembersAsync("us", "cstrike", 1, 50, "skill", true, default))
                      .ReturnsAsync(PagedResult<CountryMember>.Create([], 0, 1, 50));

        var result = await _controller.Profile("us", null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<CountryProfileViewModel>()
              .Which.Game.Should().Be("cstrike");
    }
}
