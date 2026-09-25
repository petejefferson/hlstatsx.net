using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for the games list landing page.
/// When only one non-hidden game exists the home page is shown directly;
/// when multiple games are present a selection screen is rendered instead.
/// </summary>
public interface IGameService
{
    /// <summary>Returns all visible games with their per-game stats for the games list page.</summary>
    Task<GamesListData> GetGamesListAsync(CancellationToken ct = default);
}
