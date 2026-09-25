using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Tests.Controllers;

public class HelpControllerTests
{
    private readonly Mock<IWeaponRepository> _weaponsMock;
    private readonly Mock<IGameRepository> _gamesMock;
    private readonly Mock<IPlayerService> _playersMock;
    private readonly HelpController _controller;

    public HelpControllerTests()
    {
        _weaponsMock = new Mock<IWeaponRepository>();
        _gamesMock   = new Mock<IGameRepository>();
        _playersMock = new Mock<IPlayerService>();
        _playersMock.Setup(s => s.GetModeAsync(default)).ReturnsAsync("Normal");
        _controller  = new HelpController(_weaponsMock.Object, _gamesMock.Object, _playersMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsView_WithWeaponsAndActions()
    {
        var weapons = new List<Weapon> { new() { Code = "ak47", Name = "AK-47" } };
        var actions = new List<GameAction> { new() { ActionId = 1, Description = "Headshot bonus" } };
        _weaponsMock.Setup(r => r.GetAllForHelpAsync(default)).ReturnsAsync(weapons);
        _gamesMock.Setup(r => r.GetAllActionsAsync(default)).ReturnsAsync(actions);

        var result = await _controller.Index(default);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model      = viewResult.Model.Should().BeOfType<HelpViewModel>().Subject;
        model.Weapons.Should().HaveCount(1).And.Contain(w => w.Code == "ak47");
        model.Actions.Should().HaveCount(1).And.Contain(a => a.Description == "Headshot bonus");
    }

    [Fact]
    public async Task Index_ReturnsView_WithEmptyLists_WhenNoData()
    {
        _weaponsMock.Setup(r => r.GetAllForHelpAsync(default)).ReturnsAsync(Array.Empty<Weapon>());
        _gamesMock.Setup(r => r.GetAllActionsAsync(default)).ReturnsAsync(Array.Empty<GameAction>());

        var result = await _controller.Index(default);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model      = viewResult.Model.Should().BeOfType<HelpViewModel>().Subject;
        model.Weapons.Should().BeEmpty();
        model.Actions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Normal",    "Normal")]
    [InlineData("NameTrack", "NameTrack")]
    [InlineData("LAN",       "LAN")]
    public async Task Index_PassesModeFromService_ToViewModel(string serviceMode, string expectedMode)
    {
        _weaponsMock.Setup(r => r.GetAllForHelpAsync(default)).ReturnsAsync(Array.Empty<Weapon>());
        _gamesMock.Setup(r => r.GetAllActionsAsync(default)).ReturnsAsync(Array.Empty<GameAction>());
        _playersMock.Setup(s => s.GetModeAsync(default)).ReturnsAsync(serviceMode);

        var result = await _controller.Index(default);

        var model = ((ViewResult)result).Model.Should().BeOfType<HelpViewModel>().Subject;
        model.Mode.Should().Be(expectedMode);
    }

    [Fact]
    public async Task Index_QueriesBothRepositories_Concurrently()
    {
        _weaponsMock.Setup(r => r.GetAllForHelpAsync(default)).ReturnsAsync(Array.Empty<Weapon>());
        _gamesMock.Setup(r => r.GetAllActionsAsync(default)).ReturnsAsync(Array.Empty<GameAction>());

        await _controller.Index(default);

        _weaponsMock.Verify(r => r.GetAllForHelpAsync(default), Times.Once);
        _gamesMock.Verify(r => r.GetAllActionsAsync(default), Times.Once);
    }
}
