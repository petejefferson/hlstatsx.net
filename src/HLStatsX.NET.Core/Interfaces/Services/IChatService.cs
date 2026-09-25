using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for the server chat log and per-player chat history pages.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Returns a paged list of chat messages for the given game, optionally filtered by
    /// server and/or a text search string. Mirrors <c>chat.php</c>.
    /// </summary>
    Task<PagedResult<EventChat>> GetChatLogAsync(
        string game, int? serverId, string? filter,
        string sortBy, bool desc, int page, int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of chat messages sent by a single player.
    /// Mirrors <c>chathistory.php</c>.
    /// </summary>
    Task<PagedResult<EventChat>> GetPlayerChatHistoryAsync(
        int playerId, string? filter,
        string sortBy, bool desc, int page, int pageSize,
        CancellationToken ct = default);
}
