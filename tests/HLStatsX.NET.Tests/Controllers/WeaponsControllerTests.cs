using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class WeaponsControllerTests
{
    private readonly Mock<IWeaponRepository> _weaponsMock;
    private readonly Mock<IPlayerService>    _playersMock;
    private readonly IConfiguration _config;
    private readonly WeaponsController _controller;

    public WeaponsControllerTests()
    {
        _weaponsMock = new Mock<IWeaponRepository>();
        _playersMock = new Mock<IPlayerService>();
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _controller = new WeaponsController(_weaponsMock.Object, _playersMock.Object, _config);
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithWeaponList()
    {
        var weapons = new List<Weapon>
        {
            new() { WeaponId = 1, Code = "ak47", Name = "AK-47", Game = "cstrike", Kills = 5000 }
        };
        var paged = PagedResult<Weapon>.Create(weapons, 1, 1, 50);

        _weaponsMock.Setup(r => r.GetAllAsync("cstrike", 1, 50, "kills", true, default)).ReturnsAsync(paged);
        _weaponsMock.Setup(r => r.GetKillTotalsAsync("cstrike", default)).ReturnsAsync((10000, 2000));

        var result = await _controller.Index(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<WeaponListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Weapons.Items.Should().HaveCount(1);
        model.TotalKills.Should().Be(10000);
        model.TotalHeadshots.Should().Be(2000);
    }

    [Fact]
    public async Task Index_UsesExplicitGame_WhenProvided()
    {
        _weaponsMock.Setup(r => r.GetAllAsync("dods", 1, 50, "kills", true, default))
                    .ReturnsAsync(PagedResult<Weapon>.Create([], 0, 1, 50));
        _weaponsMock.Setup(r => r.GetKillTotalsAsync("dods", default)).ReturnsAsync((0, 0));

        var result = await _controller.Index("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<WeaponListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenWeaponDoesNotExist()
    {
        _weaponsMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Weapon?)null);

        var result = await _controller.Detail(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Detail_ReturnsView_WithWeaponAndKillers()
    {
        var weapon  = new Weapon { WeaponId = 1, Code = "ak47", Name = "AK-47", Game = "cstrike", Kills = 5000 };
        var killers = PagedResult<WeaponKillerRow>.Create(
            [new(1, "FragLord", null, 100, 10)], 1, 1, 50);

        _weaponsMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(weapon);
        _weaponsMock.Setup(r => r.GetWeaponKillersAsync("ak47", "cstrike", 1, 50, "frags", true, default))
                    .ReturnsAsync(killers);
        _weaponsMock.Setup(r => r.GetWeaponKillTotalsAsync("ak47", "cstrike", default))
                    .ReturnsAsync((5000, 500));

        var result = await _controller.Detail(1);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<WeaponDetailViewModel>().Subject;
        model.Weapon.Name.Should().Be("AK-47");
        model.Killers.Items.Should().HaveCount(1);
        model.TotalKills.Should().Be(5000);
        model.DeleteDays.Should().Be(90);
    }

    [Fact]
    public async Task Detail_DeleteDays_PropagatesFromService()
    {
        var weapon = new Weapon { WeaponId = 2, Code = "knife", Name = "Knife", Game = "cstrike" };
        _weaponsMock.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(weapon);
        _weaponsMock.Setup(r => r.GetWeaponKillersAsync("knife", "cstrike", 1, 50, "frags", true, default))
                    .ReturnsAsync(PagedResult<WeaponKillerRow>.Create([], 0, 1, 50));
        _weaponsMock.Setup(r => r.GetWeaponKillTotalsAsync("knife", "cstrike", default)).ReturnsAsync((0, 0));
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(30);

        var result = await _controller.Detail(2);

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<WeaponDetailViewModel>()
              .Which.DeleteDays.Should().Be(30);
    }

    [Fact]
    public async Task Detail_PassesSortAndPage_ToRepository()
    {
        var weapon = new Weapon { WeaponId = 1, Code = "ak47", Name = "AK-47", Game = "cstrike" };
        _weaponsMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(weapon);
        _weaponsMock.Setup(r => r.GetWeaponKillersAsync("ak47", "cstrike", 3, 50, "headshots", false, default))
                    .ReturnsAsync(PagedResult<WeaponKillerRow>.Create([], 0, 3, 50));
        _weaponsMock.Setup(r => r.GetWeaponKillTotalsAsync("ak47", "cstrike", default)).ReturnsAsync((0, 0));

        await _controller.Detail(1, page: 3, sortBy: "headshots", desc: false);

        _weaponsMock.Verify(r => r.GetWeaponKillersAsync("ak47", "cstrike", 3, 50, "headshots", false, default), Times.Once);
    }
}
