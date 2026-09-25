using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

/// <summary>View model for the server chat log page (chat.php).</summary>
public record ChatLogViewModel(
    PagedResult<EventChat> Chat,
    string Game,
    int? ServerId,
    string? Filter,
    IReadOnlyList<Server> Servers,
    string SortBy,
    bool Descending);

/// <summary>View model for the per-player chat history page (chathistory.php).</summary>
public record PlayerChatViewModel(
    PagedResult<EventChat> Chat,
    int PlayerId,
    string PlayerName,
    string Game,
    string? Filter,
    string SortBy,
    bool Descending,
    int DeleteDays);
