using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class WeaponRepository : IWeaponRepository
{
    private readonly HLStatsDbContext _db;

    public WeaponRepository(HLStatsDbContext db) => _db = db;

    public async Task<Weapon?> GetByIdAsync(int weaponId, CancellationToken ct = default) =>
        await _db.Weapons.FindAsync(new object[] { weaponId }, ct);

    public async Task<Weapon?> GetByCodeAsync(string code, string game, CancellationToken ct = default) =>
        await _db.Weapons.FirstOrDefaultAsync(w => w.Code == code && w.Game == game, ct);

    public async Task<PagedResult<Weapon>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", CancellationToken ct = default)
    {
        var query = _db.Weapons.Where(w => w.Game == game && w.Kills > 0);

        query = sortBy.ToLowerInvariant() switch
        {
            "headshots" => query.OrderByDescending(w => w.Headshots),
            _ => query.OrderByDescending(w => w.Kills)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Weapon>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Weapon>> GetTopWeaponsAsync(string game, int count = 10, CancellationToken ct = default) =>
        await _db.Weapons
            .Where(w => w.Game == game && w.Kills > 0)
            .OrderByDescending(w => w.Kills)
            .Take(count)
            .ToListAsync(ct);

    public async Task UpdateAsync(Weapon weapon, CancellationToken ct = default)
    {
        _db.Weapons.Update(weapon);
        await _db.SaveChangesAsync(ct);
    }
}
