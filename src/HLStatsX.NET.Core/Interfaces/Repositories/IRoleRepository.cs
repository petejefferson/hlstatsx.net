using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByCodeAsync(string code, string game, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(string game, CancellationToken ct = default);
    Task<(int TotalKills, int TotalDeaths, int TotalPicked)> GetTotalsAsync(string game, CancellationToken ct = default);
    Task<PagedResult<RoleKillerRow>> GetRoleKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
    Task<(int TotalKills, int TotalHeadshots)> GetRoleKillTotalsAsync(string code, string game, CancellationToken ct = default);
}
