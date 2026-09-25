using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class ActionsControllerTests
{
    private readonly Mock<IActionRepository> _actionsMock;
    private readonly Mock<IPlayerService>    _playersMock;
    private readonly IConfiguration _config;
    private readonly ActionsController _controller;

    public ActionsControllerTests()
    {
        _actionsMock = new Mock<IActionRepository>();
        _playersMock = new Mock<IPlayerService>();
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new ActionsController(_actionsMock.Object, _playersMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithActionList()
    {
        var actions = new List<ActionListRow>
        {
            new("knife_kill", "Knife Kill", 250, 3)
        };
        _actionsMock.Setup(r => r.GetListAsync("cstrike", "count", true, default)).ReturnsAsync(actions);
        _actionsMock.Setup(r => r.GetTotalEarnedAsync("cstrike", default)).ReturnsAsync(1000L);

        var result = await _controller.Index(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ActionListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Actions.Should().HaveCount(1);
        model.TotalEarned.Should().Be(1000);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        _actionsMock.Setup(r => r.GetListAsync("dods", "count", true, default)).ReturnsAsync([]);
        _actionsMock.Setup(r => r.GetTotalEarnedAsync("dods", default)).ReturnsAsync(0L);

        var result = await _controller.Index("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<ActionListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Index_PassesSortByAndDesc_ToRepository()
    {
        _actionsMock.Setup(r => r.GetListAsync("cstrike", "description", false, default)).ReturnsAsync([]);
        _actionsMock.Setup(r => r.GetTotalEarnedAsync("cstrike", default)).ReturnsAsync(0L);

        await _controller.Index(null, sortBy: "description", desc: false);

        _actionsMock.Verify(r => r.GetListAsync("cstrike", "description", false, default), Times.Once);
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenActionNotFound()
    {
        _actionsMock.Setup(r => r.GetByCodeAsync("unknown", "cstrike", default))
                    .ReturnsAsync((GameAction?)null);

        var result = await _controller.Detail("unknown", "cstrike");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Detail_ReturnsView_WithAchievers_WhenNotPlayerPlayerAction()
    {
        var action = new GameAction
        {
            ActionId = 1, Code = "knife_kill", Game = "cstrike",
            Description = "Knife Kill", ForPlayerPlayerActions = false
        };
        var achievers = PagedResult<ActionAchieverRow>.Create(
            [new(1, "FragLord", null, 50, 150)], 1, 1, 40);

        _actionsMock.Setup(r => r.GetByCodeAsync("knife_kill", "cstrike", default)).ReturnsAsync(action);
        _actionsMock.Setup(r => r.GetAchieversAsync("knife_kill", "cstrike", false, 1, 40, "count", true, default))
                    .ReturnsAsync(achievers);
        _actionsMock.Setup(r => r.GetTotalAchievementsAsync("knife_kill", "cstrike", false, default))
                    .ReturnsAsync(50L);

        var result = await _controller.Detail("knife_kill", "cstrike");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ActionDetailViewModel>().Subject;
        model.Action.Description.Should().Be("Knife Kill");
        model.Achievers.Items.Should().HaveCount(1);
        model.TotalAchievements.Should().Be(50);
        model.Victims.Should().BeNull();
    }

    [Fact]
    public async Task Detail_ReturnsView_WithVictims_WhenForPlayerPlayerActions()
    {
        var action = new GameAction
        {
            ActionId = 1, Code = "backstab", Game = "cstrike",
            Description = "Backstab", ForPlayerPlayerActions = true
        };
        var achievers = PagedResult<ActionAchieverRow>.Create([], 0, 1, 40);
        var victims   = PagedResult<ActionVictimRow>.Create(
            [new(2, "Victim", null, 3, -9)], 1, 1, 40);

        _actionsMock.Setup(r => r.GetByCodeAsync("backstab", "cstrike", default)).ReturnsAsync(action);
        _actionsMock.Setup(r => r.GetAchieversAsync("backstab", "cstrike", true, 1, 40, "count", true, default))
                    .ReturnsAsync(achievers);
        _actionsMock.Setup(r => r.GetTotalAchievementsAsync("backstab", "cstrike", true, default))
                    .ReturnsAsync(3L);
        _actionsMock.Setup(r => r.GetVictimsAsync("backstab", "cstrike", 1, 40, "count", true, default))
                    .ReturnsAsync(victims);

        var result = await _controller.Detail("backstab", "cstrike");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ActionDetailViewModel>().Subject;
        model.Victims.Should().NotBeNull();
        model.Victims!.Items.Should().HaveCount(1);
        model.Victims.Items[0].PlayerName.Should().Be("Victim");
    }

    [Fact]
    public async Task Detail_UsesDefaultGame_WhenGameIsNull()
    {
        var action = new GameAction { ActionId = 1, Code = "kill", Game = "cstrike" };
        _actionsMock.Setup(r => r.GetByCodeAsync("kill", "cstrike", default)).ReturnsAsync(action);
        _actionsMock.Setup(r => r.GetAchieversAsync("kill", "cstrike", false, 1, 40, "count", true, default))
                    .ReturnsAsync(PagedResult<ActionAchieverRow>.Create([], 0, 1, 40));
        _actionsMock.Setup(r => r.GetTotalAchievementsAsync("kill", "cstrike", false, default))
                    .ReturnsAsync(0L);

        var result = await _controller.Detail("kill", null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<ActionDetailViewModel>()
              .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Detail_DeleteDays_PropagatesFromService()
    {
        var action = new GameAction { ActionId = 1, Code = "kill", Game = "cstrike" };
        _actionsMock.Setup(r => r.GetByCodeAsync("kill", "cstrike", default)).ReturnsAsync(action);
        _actionsMock.Setup(r => r.GetAchieversAsync("kill", "cstrike", false, 1, 40, "count", true, default))
                    .ReturnsAsync(PagedResult<ActionAchieverRow>.Create([], 0, 1, 40));
        _actionsMock.Setup(r => r.GetTotalAchievementsAsync("kill", "cstrike", false, default))
                    .ReturnsAsync(0L);
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(30);

        var result = await _controller.Detail("kill", "cstrike");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<ActionDetailViewModel>()
              .Which.DeleteDays.Should().Be(30);
    }
}
