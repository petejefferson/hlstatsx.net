using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IWeaponRepository
{
    Task<Weapon?> GetByIdAsync(int weaponId, CancellationToken ct = default);
    Task<Weapon?> GetByCodeAsync(string code, string game, CancellationToken ct = default);
    Task<PagedResult<Weapon>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default);
    Task<IReadOnlyList<Weapon>> GetTopWeaponsAsync(string game, int count = 10, CancellationToken ct = default);
    Task<(int TotalKills, int TotalHeadshots)> GetKillTotalsAsync(string game, CancellationToken ct = default);
    Task<PagedResult<WeaponKillerRow>> GetWeaponKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);
    Task<(int TotalKills, int TotalHeadshots)> GetWeaponKillTotalsAsync(string code, string game, CancellationToken ct = default);
    Task UpdateAsync(Weapon weapon, CancellationToken ct = default);
}
