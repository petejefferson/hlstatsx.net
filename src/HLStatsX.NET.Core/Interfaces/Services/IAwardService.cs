using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IAwardService
{
    Task<IReadOnlyList<Award>> GetAwardsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);
    Task<Rank?> GetRankForPlayerAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);
    Task<Ribbon?> GetRibbonAsync(int ribbonId, CancellationToken ct = default);
}
