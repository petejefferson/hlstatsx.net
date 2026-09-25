using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Services;

namespace HLStatsX.NET.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<IEventRepository> _eventsMock;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _eventsMock = new Mock<IEventRepository>();
        _service    = new ChatService(_eventsMock.Object);
    }

    // ── GetChatLogAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetChatLogAsync_DelegatesToRepository_WithNullPlayerId()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(null, 2, "dods", "hello", "date", true, 1, 50, default))
            .ReturnsAsync(paged);

        var result = await _service.GetChatLogAsync("dods", 2, "hello", "date", true, 1, 50);

        result.Should().BeSameAs(paged);
        _eventsMock.Verify(
            r => r.GetChatAsync(null, 2, "dods", "hello", "date", true, 1, 50, default),
            Times.Once);
    }

    [Fact]
    public async Task GetChatLogAsync_PassesNullServerId_WhenNotProvided()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(null, null, "cstrike", null, "date", false, 2, 25, default))
            .ReturnsAsync(paged);

        var result = await _service.GetChatLogAsync("cstrike", null, null, "date", false, 2, 25);

        result.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetChatLogAsync_ReturnsMessages()
    {
        var msgs = new List<EventChat>
        {
            new() { Id = 1, PlayerId = 5, Message = "gg wp", EventTime = DateTime.UtcNow, MessageMode = 0 }
        };
        var paged = PagedResult<EventChat>.Create(msgs, 1, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(null, null, "cstrike", null, "date", true, 1, 50, default))
            .ReturnsAsync(paged);

        var result = await _service.GetChatLogAsync("cstrike", null, null, "date", true, 1, 50);

        result.Items.Should().HaveCount(1);
        result.Items[0].Message.Should().Be("gg wp");
    }

    // ── GetPlayerChatHistoryAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerChatHistoryAsync_DelegatesToRepository_WithNullGameAndServer()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(7, null, null, "frag", "date", true, 1, 50, default))
            .ReturnsAsync(paged);

        var result = await _service.GetPlayerChatHistoryAsync(7, "frag", "date", true, 1, 50);

        result.Should().BeSameAs(paged);
        _eventsMock.Verify(
            r => r.GetChatAsync(7, null, null, "frag", "date", true, 1, 50, default),
            Times.Once);
    }

    [Fact]
    public async Task GetPlayerChatHistoryAsync_PassesNullFilter_WhenNotProvided()
    {
        var paged = PagedResult<EventChat>.Create([], 0, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(3, null, null, null, "date", true, 1, 50, default))
            .ReturnsAsync(paged);

        var result = await _service.GetPlayerChatHistoryAsync(3, null, "date", true, 1, 50);

        result.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetPlayerChatHistoryAsync_ReturnsMessagesForPlayer()
    {
        var msgs = new List<EventChat>
        {
            new() { Id = 10, PlayerId = 3, Message = "nice shot", EventTime = DateTime.UtcNow, MessageMode = 0 },
            new() { Id = 11, PlayerId = 3, Message = "ty",         EventTime = DateTime.UtcNow, MessageMode = 0 }
        };
        var paged = PagedResult<EventChat>.Create(msgs, 2, 1, 50);
        _eventsMock
            .Setup(r => r.GetChatAsync(3, null, null, null, "date", true, 1, 50, default))
            .ReturnsAsync(paged);

        var result = await _service.GetPlayerChatHistoryAsync(3, null, "date", true, 1, 50);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}
