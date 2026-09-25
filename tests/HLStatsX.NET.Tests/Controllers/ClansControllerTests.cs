using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class ClansControllerTests
{
    private readonly Mock<IClanService> _clansMock;
    private readonly IConfiguration _config;
    private readonly ClansController _controller;

    public ClansControllerTests()
    {
        _clansMock = new Mock<IClanService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new ClansController(_clansMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithClanLeaderboard()
    {
        var rows = new List<ClanLeaderboardRow>
        {
            new() { ClanId = 1, Name = "FragForce", Tag = "[FF]", MemberCount = 5, AvgSkill = 1200 }
        };
        var paged = PagedResult<ClanLeaderboardRow>.Create(rows, 1, 1, 50);

        _clansMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 50, "skill", true, 1, default)).ReturnsAsync(paged);
        _clansMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(1);

        var result = await _controller.Index(null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ClanLeaderboardViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Clans.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        var paged = PagedResult<ClanLeaderboardRow>.Create([], 0, 1, 50);
        _clansMock.Setup(s => s.GetLeaderboardAsync("dods", 1, 50, "skill", true, 1, default)).ReturnsAsync(paged);
        _clansMock.Setup(s => s.GetTotalCountAsync("dods", default)).ReturnsAsync(0);

        var result = await _controller.Index("dods");

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<ClanLeaderboardViewModel>()
            .Which.Game.Should().Be("dods");
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenClanDoesNotExist()
    {
        _clansMock.Setup(s => s.GetClanAsync(99, default)).ReturnsAsync((Clan?)null);

        var result = await _controller.Profile(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Profile_ReturnsNotFound_WhenSummaryDoesNotExist()
    {
        var clan = new Clan { ClanId = 1, Name = "FragForce", Game = "cstrike" };
        _clansMock.Setup(s => s.GetClanAsync(1, default)).ReturnsAsync(clan);
        _clansMock.Setup(s => s.GetSummaryAsync(1, default)).ReturnsAsync((ClanSummaryStats?)null);

        var result = await _controller.Profile(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Profile_ReturnsView_WhenClanAndSummaryExist()
    {
        var clan    = new Clan { ClanId = 1, Name = "FragForce", Game = "cstrike" };
        var summary = new ClanSummaryStats(1000, 500, 200, 7200, 3, 5, 1400, 0.8);

        _clansMock.Setup(s => s.GetClanAsync(1, default)).ReturnsAsync(clan);
        _clansMock.Setup(s => s.GetSummaryAsync(1, default)).ReturnsAsync(summary);
        _clansMock.Setup(s => s.GetFavoriteServerAsync(1, default)).ReturnsAsync((ClanFavoriteServer?)null);
        _clansMock.Setup(s => s.GetFavoriteMapAsync(1, default)).ReturnsAsync((string?)null);
        _clansMock.Setup(s => s.GetFavoriteWeaponAsync(1, "cstrike", default)).ReturnsAsync((ClanFavoriteWeapon?)null);
        _clansMock.Setup(s => s.GetMembersPagedAsync(1, "cstrike", 1, 50, "skill", true, 1000, default))
                  .ReturnsAsync(PagedResult<ClanMemberRow>.Create([], 0, 1, 50));
        _clansMock.Setup(s => s.GetWeaponUsageAsync(1, "cstrike", 1000, 200, default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetWeaponStatsAsync(1, "cstrike", default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetWeaponTargetsAsync(1, "cstrike", default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetMapPerformanceAsync(1, 1000, 200, default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetActionsAsync(1, default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetActionVictimsAsync(1, default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetTeamSelectionAsync(1, "cstrike", default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetRoleSelectionAsync(1, "cstrike", default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetMemberLocationsAsync(1, default)).ReturnsAsync([]);

        var result = await _controller.Profile(1);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ClanProfileViewModel>().Subject;
        model.Clan.Name.Should().Be("FragForce");
        model.Summary.TotalKills.Should().Be(1000);
    }
}
