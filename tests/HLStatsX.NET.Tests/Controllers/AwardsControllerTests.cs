using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class AwardsControllerTests
{
    private readonly Mock<IAwardService> _awardsMock;
    private readonly IConfiguration _config;
    private readonly AwardsController _controller;

    public AwardsControllerTests()
    {
        _awardsMock = new Mock<IAwardService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new AwardsController(_awardsMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithAllAwardData()
    {
        _awardsMock.Setup(s => s.GetDailyAwardsAsync("cstrike", default))
                   .ReturnsAsync([new() { AwardId = 1, Name = "Best Fragger" }]);
        _awardsMock.Setup(s => s.GetAwardsAsync("cstrike", default)).ReturnsAsync([]);
        _awardsMock.Setup(s => s.GetRanksWithCountsAsync("cstrike", default)).ReturnsAsync([]);
        _awardsMock.Setup(s => s.GetRibbonsWithCountsAsync("cstrike", default)).ReturnsAsync([]);

        var result = await _controller.Index(null, default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<AwardsIndexViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.DailyAwards.Should().HaveCount(1);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        _awardsMock.Setup(s => s.GetDailyAwardsAsync("dods", default)).ReturnsAsync([]);
        _awardsMock.Setup(s => s.GetAwardsAsync("dods", default)).ReturnsAsync([]);
        _awardsMock.Setup(s => s.GetRanksWithCountsAsync("dods", default)).ReturnsAsync([]);
        _awardsMock.Setup(s => s.GetRibbonsWithCountsAsync("dods", default)).ReturnsAsync([]);

        var result = await _controller.Index("dods", default);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<AwardsIndexViewModel>()
              .Which.Game.Should().Be("dods");
    }

    // ── RankDetail ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RankDetail_ReturnsNotFound_WhenRankDoesNotExist()
    {
        _awardsMock.Setup(s => s.GetRankByIdAsync(99, default)).ReturnsAsync((Rank?)null);

        var result = await _controller.RankDetail(99, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RankDetail_ReturnsView_WithPlayersForRank()
    {
        var rank    = new Rank { RankId = 1, RankName = "Private", Game = "cstrike", MinKills = 0, MaxKills = 100 };
        var players = PagedResult<RankPlayerRow>.Create(
            [new(1, "FragLord", "gb", 50, 1200)], 1, 1, 50);

        _awardsMock.Setup(s => s.GetRankByIdAsync(1, default)).ReturnsAsync(rank);
        _awardsMock.Setup(s => s.GetRankDetailAsync(1, "cstrike", 1, 50, "skill", true, default))
                   .ReturnsAsync(players);

        var result = await _controller.RankDetail(1, null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<RankDetailViewModel>().Subject;
        model.Rank.RankName.Should().Be("Private");
        model.Players.Items.Should().HaveCount(1);
    }

    // ── RibbonDetail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RibbonDetail_ReturnsNotFound_WhenRibbonDoesNotExist()
    {
        _awardsMock.Setup(s => s.GetRibbonAsync(99, default)).ReturnsAsync((Ribbon?)null);

        var result = await _controller.RibbonDetail(99, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RibbonDetail_UsesDefaultGame_WhenGameNotProvided()
    {
        var ribbon  = new Ribbon { RibbonId = 1, RibbonName = "First Blood", Game = "cstrike" };
        var players = PagedResult<RibbonDetailRow>.Create([], 0, 1, 50);
        _awardsMock.Setup(s => s.GetRibbonAsync(1, default)).ReturnsAsync(ribbon);
        _awardsMock.Setup(s => s.GetRibbonDetailAsync(1, "cstrike", 1, 50, "numawards", true, default))
                   .ReturnsAsync(players);

        var result = await _controller.RibbonDetail(1, null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<RibbonDetailViewModel>()
              .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task RibbonDetail_ReturnsView_WithRibbon()
    {
        var ribbon  = new Ribbon { RibbonId = 1, RibbonName = "First Blood", Game = "cstrike" };
        var players = PagedResult<RibbonDetailRow>.Create([], 0, 1, 50);
        _awardsMock.Setup(s => s.GetRibbonAsync(1, default)).ReturnsAsync(ribbon);
        _awardsMock.Setup(s => s.GetRibbonDetailAsync(1, "cstrike", 1, 50, "numawards", true, default))
                   .ReturnsAsync(players);

        var result = await _controller.RibbonDetail(1, null, 1, "numawards", true, default);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<RibbonDetailViewModel>().Subject;
        model.Ribbon.RibbonName.Should().Be("First Blood");
    }

    [Fact]
    public async Task RibbonDetail_PassesSortAndPageParametersThrough()
    {
        var ribbon  = new Ribbon { RibbonId = 2, RibbonName = "Ace", Game = "cstrike" };
        var players = PagedResult<RibbonDetailRow>.Create([], 0, 2, 50);
        _awardsMock.Setup(s => s.GetRibbonAsync(2, default)).ReturnsAsync(ribbon);
        _awardsMock.Setup(s => s.GetRibbonDetailAsync(2, "cstrike", 2, 50, "player", false, default))
                   .ReturnsAsync(players);

        var result = await _controller.RibbonDetail(2, null, page: 2, sortBy: "player", desc: false, default);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
                          .Should().BeOfType<RibbonDetailViewModel>().Subject;
        model.SortBy.Should().Be("player");
        model.Descending.Should().BeFalse();
        model.Players.Page.Should().Be(2);
    }

    [Fact]
    public async Task RibbonDetail_ReturnsPlayerLeaderboard_WhenPlayersExist()
    {
        var ribbon  = new Ribbon { RibbonId = 1, RibbonName = "First Blood", Game = "cstrike" };
        var rows    = new List<RibbonDetailRow>
        {
            new(1, "FragLord", "gb", 10, "Most kills with ak47"),
            new(2, "Headshotz", "us",  7, "Most kills with ak47"),
        };
        var players = PagedResult<RibbonDetailRow>.Create(rows, 2, 1, 50);
        _awardsMock.Setup(s => s.GetRibbonAsync(1, default)).ReturnsAsync(ribbon);
        _awardsMock.Setup(s => s.GetRibbonDetailAsync(1, "cstrike", 1, 50, "numawards", true, default))
                   .ReturnsAsync(players);

        var result = await _controller.RibbonDetail(1, null, 1, "numawards", true, default);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
                          .Should().BeOfType<RibbonDetailViewModel>().Subject;
        model.Players.TotalCount.Should().Be(2);
        model.Players.Items[0].PlayerName.Should().Be("FragLord");
        model.Players.Items[0].NumAwards.Should().Be(10);
        model.Players.Items[1].PlayerName.Should().Be("Headshotz");
    }

    // ── DailyAwardDetail ─────────────────────────────────────────────────────

    [Fact]
    public async Task DailyAwardDetail_ReturnsNotFound_WhenAwardDoesNotExist()
    {
        _awardsMock.Setup(s => s.GetAwardByIdAsync(99, default)).ReturnsAsync((Award?)null);

        var result = await _controller.DailyAwardDetail(99, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DailyAwardDetail_ReturnsView_WithAwardAndHistory()
    {
        var award   = new Award { AwardId = 1, Name = "Best Fragger", Game = "cstrike" };
        var history = PagedResult<DailyAwardHistoryRow>.Create(
            [new(1, DateTime.Today, "FragLord", null, 50)], 1, 1, 50);

        _awardsMock.Setup(s => s.GetAwardByIdAsync(1, default)).ReturnsAsync(award);
        _awardsMock.Setup(s => s.GetDailyAwardHistoryAsync(1, 1, 50, "awardTime", true, default))
                   .ReturnsAsync(history);

        var result = await _controller.DailyAwardDetail(1, null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<DailyAwardDetailViewModel>().Subject;
        model.Award.Name.Should().Be("Best Fragger");
        model.History.Items.Should().HaveCount(1);
    }
}
