using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IRoleRepository> _rolesMock;
    private readonly Mock<IPlayerService>  _playersMock;
    private readonly IConfiguration _config;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _rolesMock   = new Mock<IRoleRepository>();
        _playersMock = new Mock<IPlayerService>();
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new RolesController(_rolesMock.Object, _playersMock.Object, _config);
    }

    private void SetupIndexMocks(string game, IReadOnlyList<Role>? roles = null)
    {
        roles ??= [];
        _rolesMock.Setup(r => r.GetAllAsync(game, default)).ReturnsAsync(roles);
        _rolesMock.Setup(r => r.GetTotalsAsync(game, default)).ReturnsAsync((1000, 800, 2000));
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithRoleList()
    {
        var roles = new List<Role>
        {
            new() { RoleId = 1, Code = "ct",  Name = "Counter-Terrorist", Game = "cstrike", Kills = 500 },
            new() { RoleId = 2, Code = "t",   Name = "Terrorist",         Game = "cstrike", Kills = 400 }
        };
        SetupIndexMocks("cstrike", roles);

        var result = await _controller.Index(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<RoleListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Roles.Should().HaveCount(2);
        model.TotalKills.Should().Be(1000);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        SetupIndexMocks("dods");

        var result = await _controller.Index("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<RoleListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Index_SortsByKills_Descending_ByDefault()
    {
        var roles = new List<Role>
        {
            new() { RoleId = 1, Code = "t",  Name = "Terrorist",         Kills = 300, Game = "cstrike" },
            new() { RoleId = 2, Code = "ct", Name = "Counter-Terrorist", Kills = 600, Game = "cstrike" },
        };
        SetupIndexMocks("cstrike", roles);

        var result = await _controller.Index(null, sortBy: "kills", desc: true);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<RoleListViewModel>().Subject;
        model.Roles[0].Kills.Should().Be(600);
        model.Roles[1].Kills.Should().Be(300);
    }

    [Fact]
    public async Task Index_SortsByName_WhenSortByIsRole()
    {
        var roles = new List<Role>
        {
            new() { RoleId = 1, Code = "t",  Name = "Terrorist",         Kills = 300, Game = "cstrike" },
            new() { RoleId = 2, Code = "ct", Name = "Counter-Terrorist", Kills = 600, Game = "cstrike" },
        };
        SetupIndexMocks("cstrike", roles);

        var result = await _controller.Index(null, sortBy: "role", desc: false);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<RoleListViewModel>().Subject;
        model.Roles[0].Name.Should().Be("Counter-Terrorist");
        model.Roles[1].Name.Should().Be("Terrorist");
    }

    [Fact]
    public async Task Index_SortsByKd_Descending()
    {
        var roles = new List<Role>
        {
            new() { RoleId = 1, Code = "t",  Name = "Terrorist",         Kills = 200, Deaths = 400, Game = "cstrike" },
            new() { RoleId = 2, Code = "ct", Name = "Counter-Terrorist", Kills = 400, Deaths = 200, Game = "cstrike" },
        };
        SetupIndexMocks("cstrike", roles);

        var result = await _controller.Index(null, sortBy: "kd", desc: true);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<RoleListViewModel>().Subject;
        // CT has 2.0 K/D, T has 0.5 K/D → CT first when descending
        model.Roles[0].Code.Should().Be("ct");
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenCodeIsNull()
    {
        var result = await _controller.Detail(null, "cstrike");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenRoleNotFound()
    {
        _rolesMock.Setup(r => r.GetByCodeAsync("scout", "cstrike", default)).ReturnsAsync((Role?)null);

        var result = await _controller.Detail("scout", "cstrike");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Detail_ReturnsView_WithRoleAndKillers()
    {
        var role    = new Role { RoleId = 1, Code = "ct", Name = "Counter-Terrorist", Game = "cstrike", Kills = 500 };
        var killers = PagedResult<RoleKillerRow>.Create(
            [new(1, "FragLord", null, 100)], 1, 1, 50);

        _rolesMock.Setup(r => r.GetByCodeAsync("ct", "cstrike", default)).ReturnsAsync(role);
        _rolesMock.Setup(r => r.GetRoleKillersAsync("ct", "cstrike", 1, 50, "frags", true, default))
                  .ReturnsAsync(killers);
        _rolesMock.Setup(r => r.GetRoleKillTotalsAsync("ct", "cstrike", default))
                  .ReturnsAsync((500, 50));

        var result = await _controller.Detail("ct", "cstrike");

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<RoleDetailViewModel>().Subject;
        model.Role.Name.Should().Be("Counter-Terrorist");
        model.Killers.Items.Should().HaveCount(1);
        model.TotalKills.Should().Be(500);
        model.TotalHeadshots.Should().Be(50);
    }

    [Fact]
    public async Task Detail_UsesDefaultGame_WhenGameIsNull()
    {
        var role = new Role { RoleId = 1, Code = "ct", Name = "Counter-Terrorist", Game = "cstrike" };
        _rolesMock.Setup(r => r.GetByCodeAsync("ct", "cstrike", default)).ReturnsAsync(role);
        _rolesMock.Setup(r => r.GetRoleKillersAsync("ct", "cstrike", 1, 50, "frags", true, default))
                  .ReturnsAsync(PagedResult<RoleKillerRow>.Create([], 0, 1, 50));
        _rolesMock.Setup(r => r.GetRoleKillTotalsAsync("ct", "cstrike", default)).ReturnsAsync((0, 0));

        var result = await _controller.Detail("ct", null);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<RoleDetailViewModel>()
              .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Detail_DeleteDays_PropagatesFromService()
    {
        var role = new Role { RoleId = 1, Code = "ct", Name = "Counter-Terrorist", Game = "cstrike" };
        _rolesMock.Setup(r => r.GetByCodeAsync("ct", "cstrike", default)).ReturnsAsync(role);
        _rolesMock.Setup(r => r.GetRoleKillersAsync("ct", "cstrike", 1, 50, "frags", true, default))
                  .ReturnsAsync(PagedResult<RoleKillerRow>.Create([], 0, 1, 50));
        _rolesMock.Setup(r => r.GetRoleKillTotalsAsync("ct", "cstrike", default)).ReturnsAsync((0, 0));
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(14);

        var result = await _controller.Detail("ct", "cstrike");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<RoleDetailViewModel>()
              .Which.DeleteDays.Should().Be(14);
    }
}
