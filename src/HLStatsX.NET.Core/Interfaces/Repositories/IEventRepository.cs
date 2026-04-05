using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PagedResult<EventFrag>> GetFragsAsync(int? playerId = null, int? serverId = null, string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<EventChat>> GetChatAsync(int? playerId = null, int? serverId = null, string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<EventFrag>> GetRecentKillsAsync(int playerId, int count = 20, CancellationToken ct = default);
    Task<IReadOnlyList<EventFrag>> GetTopVictimsAsync(int killerId, int count = 10, CancellationToken ct = default);
    Task<IReadOnlyList<EventFrag>> GetTopKillersOfPlayerAsync(int victimId, int count = 10, CancellationToken ct = default);
    Task AddFragAsync(EventFrag frag, CancellationToken ct = default);
    Task AddChatAsync(EventChat chat, CancellationToken ct = default);
    Task AddConnectAsync(EventConnect connect, CancellationToken ct = default);
    Task AddDisconnectAsync(EventDisconnect disconnect, CancellationToken ct = default);
}
