using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

/// <summary>
/// Serves the server chat log (<c>chat.php</c>) and per-player chat history (<c>chathistory.php</c>).
/// </summary>
public class ChatController : Controller
{
    private readonly IChatService _chat;
    private readonly IServerService _servers;
    private readonly IPlayerService _players;
    private readonly IConfiguration _config;

    public ChatController(IChatService chat, IServerService servers, IPlayerService players, IConfiguration config)
    {
        _chat = chat;
        _servers = servers;
        _players = players;
        _config = config;
    }

    /// <summary>
    /// Server chat log — shows all recent chat messages for the game, optionally filtered
    /// by server and/or a text search string.
    /// </summary>
    public async Task<IActionResult> Index(
        string? game, int? serverId, string? filter,
        string sortBy = "date", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"]!;
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var chatTask    = _chat.GetChatLogAsync(game, serverId, filter, sortBy, desc, page, pageSize, ct);
        var serversTask = _servers.GetServersAsync(game, ct);
        await Task.WhenAll(chatTask, serversTask);

        return View(new ChatLogViewModel(
            await chatTask, game, serverId, filter, await serversTask, sortBy, desc));
    }

    /// <summary>
    /// Per-player chat history — shows all chat messages sent by a single player,
    /// optionally filtered by a text search string.
    /// </summary>
    public async Task<IActionResult> PlayerHistory(
        int id, string? filter,
        string sortBy = "date", bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var player = await _players.GetPlayerAsync(id, ct);
        if (player == null) return NotFound();

        var chatTask       = _chat.GetPlayerChatHistoryAsync(id, filter, sortBy, desc, page, pageSize, ct);
        var deleteDaysTask = _players.GetDeleteDaysAsync(ct);
        await Task.WhenAll(chatTask, deleteDaysTask);

        return View(new PlayerChatViewModel(
            await chatTask, id, player.LastName, player.Game, filter, sortBy, desc, await deleteDaysTask));
    }
}
