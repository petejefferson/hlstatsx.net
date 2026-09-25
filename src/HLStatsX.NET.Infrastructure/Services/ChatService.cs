using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IEventRepository _events;

    public ChatService(IEventRepository events) => _events = events;

    public Task<PagedResult<EventChat>> GetChatLogAsync(
        string game, int? serverId, string? filter,
        string sortBy, bool desc, int page, int pageSize,
        CancellationToken ct = default)
        => _events.GetChatAsync(null, serverId, game, filter, sortBy, desc, page, pageSize, ct);

    public Task<PagedResult<EventChat>> GetPlayerChatHistoryAsync(
        int playerId, string? filter,
        string sortBy, bool desc, int page, int pageSize,
        CancellationToken ct = default)
        => _events.GetChatAsync(playerId, null, null, filter, sortBy, desc, page, pageSize, ct);
}
