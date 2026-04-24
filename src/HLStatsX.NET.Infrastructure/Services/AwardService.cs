using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Infrastructure.Services;

public class AwardService : IAwardService
{
    private readonly IAwardRepository _awards;
    private readonly IMemoryCache _cache;

    // Ranks and ribbons are static reference data — cache aggressively.
    private static readonly TimeSpan RankCacheTtl   = TimeSpan.FromHours(1);
    private static readonly TimeSpan RibbonCacheTtl = TimeSpan.FromHours(1);

    public AwardService(IAwardRepository awards, IMemoryCache cache)
    {
        _awards = awards;
        _cache  = cache;
    }

    public Task<IReadOnlyList<Award>> GetAwardsAsync(string game, CancellationToken ct = default) =>
        _awards.GetAllAsync(game, ct);

    public Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default) =>
        _awards.GetDailyAwardsAsync(game, ct);

    /// <summary>Returns all ranks for the game, served from cache after the first load.</summary>
    public async Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default)
    {
        var key = $"ranks:{game}";
        if (_cache.TryGetValue(key, out IReadOnlyList<Rank>? cached))
            return cached!;

        var ranks = await _awards.GetRanksAsync(game, ct);
        _cache.Set(key, ranks, RankCacheTtl);
        return ranks;
    }

    /// <summary>
    /// Resolves the rank for the given kill count.
    /// Delegates to the cached rank list so no extra DB query is needed.
    /// </summary>
    public async Task<Rank?> GetRankForPlayerAsync(int playerId, string game, int kills, CancellationToken ct = default)
    {
        var ranks = await GetRanksAsync(game, ct);
        // Highest rank whose MinKills threshold the player has reached
        return ranks.Where(r => r.MinKills <= kills).MaxBy(r => r.MinKills);
    }

    /// <summary>Returns all ribbons for the game, served from cache after the first load.</summary>
    public async Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default)
    {
        var key = $"ribbons:{game}";
        if (_cache.TryGetValue(key, out IReadOnlyList<Ribbon>? cached))
            return cached!;

        var ribbons = await _awards.GetRibbonsAsync(game, ct);
        _cache.Set(key, ribbons, RibbonCacheTtl);
        return ribbons;
    }

    public Task<Ribbon?> GetRibbonAsync(int ribbonId, CancellationToken ct = default) =>
        _awards.GetRibbonByIdAsync(ribbonId, ct);

    public Task<IReadOnlyList<RankRow>> GetRanksWithCountsAsync(string game, CancellationToken ct = default) =>
        _awards.GetRanksWithCountsAsync(game, ct);

    public Task<IReadOnlyList<RibbonRow>> GetRibbonsWithCountsAsync(string game, CancellationToken ct = default) =>
        _awards.GetRibbonsWithCountsAsync(game, ct);
}
