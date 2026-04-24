using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IActionRepository
{
    Task<IReadOnlyList<ActionListRow>> GetListAsync(string game, string sortBy, bool desc, CancellationToken ct = default);
    Task<long> GetTotalEarnedAsync(string game, CancellationToken ct = default);
    Task<GameAction?> GetByCodeAsync(string code, string game, CancellationToken ct = default);
    Task<PagedResult<ActionAchieverRow>> GetAchieversAsync(string code, string game, bool usePlayerPlayerActions, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
    Task<long> GetTotalAchievementsAsync(string code, string game, bool usePlayerPlayerActions, CancellationToken ct = default);
    Task<PagedResult<ActionVictimRow>> GetVictimsAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
}
