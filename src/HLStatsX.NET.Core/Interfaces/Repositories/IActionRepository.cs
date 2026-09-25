using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for game actions (in-game events such as bomb plants,
/// flag captures, or assists) stored in <c>hlstats_Actions</c>.
/// Actions may involve one player (<c>hlstats_Events_PlayerActions</c>) or two
/// players (<c>hlstats_Events_PlayerPlayerActions</c>); <paramref name="usePlayerPlayerActions"/>
/// selects which table to query.
/// </summary>
public interface IActionRepository
{
    /// <summary>Returns all actions for the game with aggregate totals, sorted and ordered.</summary>
    Task<IReadOnlyList<ActionListRow>> GetListAsync(string game, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns the sum of all skill points earned from actions across the entire game.</summary>
    Task<long> GetTotalEarnedAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a single action definition by its code string and game, or <c>null</c> if not found.</summary>
    Task<GameAction?> GetByCodeAsync(string code, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged leaderboard of players who have performed this action the most.
    /// When <paramref name="usePlayerPlayerActions"/> is <c>true</c>, queries the two-player event table.
    /// </summary>
    Task<PagedResult<ActionAchieverRow>> GetAchieversAsync(string code, string game, bool usePlayerPlayerActions, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns the total number of times an action has been performed (for pagination).</summary>
    Task<long> GetTotalAchievementsAsync(string code, string game, bool usePlayerPlayerActions, CancellationToken ct = default);

    /// <summary>Returns the players most often on the receiving end of a two-player action.</summary>
    Task<PagedResult<ActionVictimRow>> GetVictimsAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
}
