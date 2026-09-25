using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for querying and writing individual game events
/// (kills, chat messages, connect/disconnect records).
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Returns a paged list of frag events, optionally filtered by player, server, or game.
    /// All filter parameters are optional — omitting them returns all frags for the game.
    /// </summary>
    Task<PagedResult<EventFrag>> GetFragsAsync(int? playerId = null, int? serverId = null, string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of chat messages, optionally filtered by player, server, game, or text.
    /// When <paramref name="game"/> is set the query joins to Servers to restrict to that game's servers.
    /// </summary>
    Task<PagedResult<EventChat>> GetChatAsync(int? playerId = null, int? serverId = null, string? game = null,
        string? filter = null, string sortBy = "date", bool desc = true,
        int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>Returns the most recent kills by a specific player.</summary>
    Task<IReadOnlyList<EventFrag>> GetRecentKillsAsync(int playerId, int count = 20, CancellationToken ct = default);

    /// <summary>Returns the players a killer has killed the most (top victims).</summary>
    Task<IReadOnlyList<EventFrag>> GetTopVictimsAsync(int killerId, int count = 10, CancellationToken ct = default);

    /// <summary>Returns the players who have killed a given player the most (nemesis list).</summary>
    Task<IReadOnlyList<EventFrag>> GetTopKillersOfPlayerAsync(int victimId, int count = 10, CancellationToken ct = default);

    /// <summary>Inserts a frag event record.</summary>
    Task AddFragAsync(EventFrag frag, CancellationToken ct = default);
    /// <summary>Inserts a chat message record.</summary>
    Task AddChatAsync(EventChat chat, CancellationToken ct = default);
    /// <summary>Inserts a connect event record.</summary>
    Task AddConnectAsync(EventConnect connect, CancellationToken ct = default);
    /// <summary>Inserts a disconnect event record.</summary>
    Task AddDisconnectAsync(EventDisconnect disconnect, CancellationToken ct = default);
}
