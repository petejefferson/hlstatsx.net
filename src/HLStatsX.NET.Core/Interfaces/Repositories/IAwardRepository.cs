using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IAwardRepository
{
    Task<Award?> GetByIdAsync(int awardId, CancellationToken ct = default);
    Task<IReadOnlyList<Award>> GetAllAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default);
    Task<Rank?> GetRankForKillsAsync(string game, int kills, CancellationToken ct = default);
    Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default);
    Task<Ribbon?> GetRibbonByIdAsync(int ribbonId, CancellationToken ct = default);
}
