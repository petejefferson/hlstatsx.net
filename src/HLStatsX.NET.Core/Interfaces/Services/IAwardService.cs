using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for awards, daily award history, ranks, and ribbons.
/// </summary>
public interface IAwardService
{
    /// <summary>Returns all awards defined for the game.</summary>
    Task<IReadOnlyList<Award>> GetAwardsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns the subset of awards that are distributed on a daily basis.</summary>
    Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all rank tiers for the game, ordered by kill threshold ascending.</summary>
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the appropriate rank for a player given their kill count,
    /// or <c>null</c> if they have not yet reached the first tier.
    /// </summary>
    Task<Rank?> GetRankForPlayerAsync(int playerId, string game, int kills, CancellationToken ct = default);

    /// <summary>Returns all ranks with the number of players currently holding each one.</summary>
    Task<IReadOnlyList<RankRow>> GetRanksWithCountsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all ribbons defined for the game.</summary>
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a single ribbon by primary key, or <c>null</c> if not found.</summary>
    Task<Ribbon?> GetRibbonAsync(int ribbonId, CancellationToken ct = default);

    /// <summary>Returns all ribbons with the number of players who have earned each one.</summary>
    Task<IReadOnlyList<RibbonRow>> GetRibbonsWithCountsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a single award by primary key, or <c>null</c> if not found.</summary>
    Task<Award?> GetAwardByIdAsync(int awardId, CancellationToken ct = default);

    /// <summary>Returns the paged history of daily award winners for the given award.</summary>
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
