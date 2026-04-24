using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IAwardService
{
    Task<IReadOnlyList<Award>> GetAwardsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);
    Task<Rank?> GetRankForPlayerAsync(int playerId, string game, int kills, CancellationToken ct = default);
    Task<IReadOnlyList<RankRow>> GetRanksWithCountsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);
    Task<Ribbon?> GetRibbonAsync(int ribbonId, CancellationToken ct = default);
    Task<IReadOnlyList<RibbonRow>> GetRibbonsWithCountsAsync(string game, CancellationToken ct = default);
    Task<Award?> GetAwardByIdAsync(int awardId, CancellationToken ct = default);
    Task<PagedResult<DailyAwardHistoryRow>> GetDailyAwardHistoryAsync(int awardId, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
}
