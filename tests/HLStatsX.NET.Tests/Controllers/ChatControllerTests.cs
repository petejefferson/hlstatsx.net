using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _chatMock;
    private readonly Mock<IServerService> _serversMock;
    private readonly Mock<IPlayerService> _playersMock;
    private readonly IConfiguration _config;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _chatMock    = new Mock<IChatService>();
        _serversMock = new Mock<IServerService>();
        _playersMock = new Mock<IPlayerService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]     = "cstrike",
                ["HLStatsX:DefaultPageSize"] = "50"
            })
            .Build();

        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(90);
        _controller = new ChatController(_chatMock.Object, _serversMock.Object, _playersMock.Object, _config);
    }

    // ── Index (server chat log) ───────────────────────────────────────────────

    [Fact]
    public async Task Index_UsesDefaultGame_WhenGameNotProvided()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _chatMock.Setup(s => s.GetChatLogAsync("cstrike", null, null, "date", true, 1, 50, default)).ReturnsAsync(paged);
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);

        var result = await _controller.Index(null, null, null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<ChatLogViewModel>()
            .Which.Game.Should().Be("cstrike");
    }

    [Fact]
    public async Task Index_UsesExplicitGame()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _chatMock.Setup(s => s.GetChatLogAsync("dods", null, null, "date", true, 1, 50, default)).ReturnsAsync(paged);
        _serversMock.Setup(s => s.GetServersAsync("dods", default)).ReturnsAsync([]);

        var result = await _controller.Index("dods", null, null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<ChatLogViewModel>()
            .Which.Game.Should().Be("dods");
    }

    [Fact]
    public async Task Index_PassesServerIdAndFilter()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _chatMock.Setup(s => s.GetChatLogAsync("cstrike", 3, "hello", "date", true, 1, 50, default)).ReturnsAsync(paged);
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);

        var result = await _controller.Index("cstrike", 3, "hello");

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<ChatLogViewModel>().Subject;
        vm.ServerId.Should().Be(3);
        vm.Filter.Should().Be("hello");
    }

    [Fact]
    public async Task Index_PopulatesServerList()
    {
        var servers = new List<Server> { new() { ServerId = 1, Name = "Server A", Game = "cstrike" } };
        var paged   = PagedResult<EventChat>.Create([], 0, 1, 50);
        _chatMock.Setup(s => s.GetChatLogAsync("cstrike", null, null, "date", true, 1, 50, default)).ReturnsAsync(paged);
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync(servers);

        var result = await _controller.Index(null, null, null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<ChatLogViewModel>().Subject;
        vm.Servers.Should().HaveCount(1);
        vm.Servers[0].Name.Should().Be("Server A");
    }

    // ── PlayerHistory ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PlayerHistory_ReturnsNotFound_WhenPlayerMissing()
    {
        _playersMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.PlayerHistory(99, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PlayerHistory_ReturnsView_WithPlayerNameAndChat()
    {
        var player = new Player { PlayerId = 5, LastName = "FragLord", Game = "cstrike" };
        var msgs   = new List<EventChat>
        {
            new() { Id = 1, PlayerId = 5, Message = "hello", EventTime = DateTime.UtcNow, MessageMode = 0 }
        };
        var paged = PagedResult<EventChat>.Create(msgs, 1, 1, 50);
        _playersMock.Setup(s => s.GetPlayerAsync(5, default)).ReturnsAsync(player);
        _chatMock.Setup(s => s.GetPlayerChatHistoryAsync(5, null, "date", true, 1, 50, default)).ReturnsAsync(paged);

        var result = await _controller.PlayerHistory(5, null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<PlayerChatViewModel>().Subject;
        vm.PlayerName.Should().Be("FragLord");
        vm.Chat.Items.Should().HaveCount(1);
        vm.Chat.Items[0].Message.Should().Be("hello");
    }

    [Fact]
    public async Task PlayerHistory_PassesFilter()
    {
        var player = new Player { PlayerId = 5, LastName = "FragLord", Game = "cstrike" };
        var paged  = PagedResult<EventChat>.Create([], 0, 1, 50);
        _playersMock.Setup(s => s.GetPlayerAsync(5, default)).ReturnsAsync(player);
        _chatMock.Setup(s => s.GetPlayerChatHistoryAsync(5, "gg", "date", true, 1, 50, default)).ReturnsAsync(paged);

        var result = await _controller.PlayerHistory(5, "gg");

        var vm = ((ViewResult)result).Model.Should().BeOfType<PlayerChatViewModel>().Subject;
        vm.Filter.Should().Be("gg");
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(180)]
    public async Task PlayerHistory_PassesDeleteDaysFromService(int days)
    {
        var player = new Player { PlayerId = 5, LastName = "FragLord", Game = "cstrike" };
        var paged  = PagedResult<EventChat>.Create([], 0, 1, 50);
        _playersMock.Setup(s => s.GetPlayerAsync(5, default)).ReturnsAsync(player);
        _playersMock.Setup(s => s.GetDeleteDaysAsync(default)).ReturnsAsync(days);
        _chatMock.Setup(s => s.GetPlayerChatHistoryAsync(5, null, "date", true, 1, 50, default)).ReturnsAsync(paged);

        var result = await _controller.PlayerHistory(5, null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<PlayerChatViewModel>().Subject;
        vm.DeleteDays.Should().Be(days);
    }
}
