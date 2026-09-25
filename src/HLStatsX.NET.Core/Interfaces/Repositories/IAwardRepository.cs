using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for awards, ranks, and ribbons.
/// Awards are per-day records stored against each player; ranks are unlocked by career
/// kill count; ribbons are milestone badges triggered by configurable criteria.
/// </summary>
public interface IAwardRepository
{
    /// <summary>Returns a single award by primary key, or <c>null</c> if not found.</summary>
    Task<Award?> GetByIdAsync(int awardId, CancellationToken ct = default);

    /// <summary>Returns all awards defined for the given game.</summary>
    Task<IReadOnlyList<Award>> GetAllAsync(string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the subset of awards that are distributed on a daily basis
    /// (e.g. most kills in a day, most headshots in a day).
    /// </summary>
    Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all ranks defined for the game, ordered by required kills ascending.</summary>
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the highest rank whose kill threshold the given <paramref name="kills"/> count meets,
    /// or <c>null</c> if the player has not yet reached the first rank tier.
    /// </summary>
    Task<Rank?> GetRankForKillsAsync(string game, int kills, CancellationToken ct = default);

    /// <summary>Returns all ranks with the number of players currently holding each one.</summary>
    Task<IReadOnlyList<RankRow>> GetRanksWithCountsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all ribbons defined for the game.</summary>
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a single ribbon by primary key, or <c>null</c> if not found.</summary>
    Task<Ribbon?> GetRibbonByIdAsync(int ribbonId, CancellationToken ct = default);

    /// <summary>Returns all ribbons with the number of players who have earned each one.</summary>
    Task<IReadOnlyList<RibbonRow>> GetRibbonsWithCountsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns the history of daily award winners for a given award, paged and sorted.</summary>
    Task<PagedResult<DailyAwardHistoryRow>> GetDailyAwardHistoryAsync(int awardId, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns a single rank by primary key, or <c>null</c> if not found.</summary>
    Task<Rank?> GetRankByIdAsync(int rankId, CancellationToken ct = default);

    /// <summary>Returns a paged list of players currently holding the given rank.</summary>
    Task<PagedResult<RankPlayerRow>> GetRankDetailAsync(int rankId, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns a paged leaderboard of players who have earned the given ribbon.</summary>
    Task<PagedResult<RibbonDetailRow>> GetRibbonDetailAsync(int ribbonId, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns the date of the last daily awards run and the number of days it covers.</summary>
    Task<(DateTime? Date, int NumDays)> GetAwardDateInfoAsync(CancellationToken ct = default);
}
