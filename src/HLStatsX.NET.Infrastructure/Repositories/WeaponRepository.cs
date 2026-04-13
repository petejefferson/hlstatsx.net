using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class WeaponRepository : IWeaponRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public WeaponRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Weapon?> GetByIdAsync(int weaponId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Weapons.FindAsync(new object[] { weaponId }, ct);
    }

    public async Task<Weapon?> GetByCodeAsync(string code, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Weapons.FirstOrDefaultAsync(w => w.Code == code && w.Game == game, ct);
    }

    public async Task<PagedResult<Weapon>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.Weapons.Where(w => w.Game == game && w.Kills > 0);

        query = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("modifier",  true)  => query.OrderByDescending(w => w.Modifier),
            ("modifier",  false) => query.OrderBy(w => w.Modifier),
            ("kills",     true)  => query.OrderByDescending(w => w.Kills),
            ("kills",     false) => query.OrderBy(w => w.Kills),
            ("kpercent",  true)  => query.OrderByDescending(w => w.Kills),
            ("kpercent",  false) => query.OrderBy(w => w.Kills),
            ("headshots", true)  => query.OrderByDescending(w => w.Headshots),
            ("headshots", false) => query.OrderBy(w => w.Headshots),
            ("hpercent",  true)  => query.OrderByDescending(w => w.Headshots),
            ("hpercent",  false) => query.OrderBy(w => w.Headshots),
            ("hpk",       true)  => query.OrderByDescending(w => w.Kills == 0 ? 0.0 : (double)w.Headshots / w.Kills),
            ("hpk",       false) => query.OrderBy(w => w.Kills == 0 ? 0.0 : (double)w.Headshots / w.Kills),
            ("hspct",     true)  => query.OrderByDescending(w => w.Kills == 0 ? 0.0 : (double)w.Headshots / w.Kills),
            ("hspct",     false) => query.OrderBy(w => w.Kills == 0 ? 0.0 : (double)w.Headshots / w.Kills),
            (_,           true)  => query.OrderByDescending(w => w.Kills),
            (_,           false) => query.OrderBy(w => w.Kills)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Weapon>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Weapon>> GetTopWeaponsAsync(string game, int count = 10, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Weapons
            .Where(w => w.Game == game && w.Kills > 0)
            .OrderByDescending(w => w.Kills)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<(int TotalKills, int TotalHeadshots)> GetKillTotalsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var totals = await db.Weapons
            .Where(w => w.Game == game)
            .GroupBy(_ => 1)
            .Select(g => new { Kills = (int?)g.Sum(w => w.Kills), Headshots = (int?)g.Sum(w => w.Headshots) })
            .FirstOrDefaultAsync(ct);
        return (totals?.Kills ?? 0, totals?.Headshots ?? 0);
    }

    public async Task UpdateAsync(Weapon weapon, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Weapons.Update(weapon);
        await db.SaveChangesAsync(ct);
    }
}
