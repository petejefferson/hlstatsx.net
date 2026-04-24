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

    public async Task<PagedResult<WeaponKillerRow>> GetWeaponKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var aggregated = db.EventFrags
            .Where(f => f.Weapon == code)
            .Join(db.Players, f => f.KillerId, p => p.PlayerId, (f, p) => new { f, p })
            .Where(x => x.p.Game == game && x.p.HideRanking == 0)
            .GroupBy(x => new { x.f.KillerId, x.p.LastName, x.p.Flag })
            .Select(g => new
            {
                PlayerId   = g.Key.KillerId,
                PlayerName = g.Key.LastName,
                Flag       = g.Key.Flag,
                Frags      = g.Count(),
                Headshots  = g.Sum(x => x.f.Headshot ? 1 : 0)
            });

        aggregated = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("player",    true)  => aggregated.OrderByDescending(r => r.PlayerName),
            ("player",    false) => aggregated.OrderBy(r => r.PlayerName),
            ("headshots", true)  => aggregated.OrderByDescending(r => r.Headshots).ThenBy(r => r.PlayerName),
            ("headshots", false) => aggregated.OrderBy(r => r.Headshots).ThenBy(r => r.PlayerName),
            ("hpk",       true)  => aggregated.OrderByDescending(r => r.Frags == 0 ? 0.0 : (double)r.Headshots / r.Frags).ThenBy(r => r.PlayerName),
            ("hpk",       false) => aggregated.OrderBy(r => r.Frags == 0 ? 0.0 : (double)r.Headshots / r.Frags).ThenBy(r => r.PlayerName),
            (_,           true)  => aggregated.OrderByDescending(r => r.Frags).ThenBy(r => r.PlayerName),
            (_,           false) => aggregated.OrderBy(r => r.Frags).ThenBy(r => r.PlayerName)
        };

        var total = await aggregated.CountAsync(ct);
        var rows  = await aggregated.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = rows.Select(r => new WeaponKillerRow(r.PlayerId, r.PlayerName, r.Flag, r.Frags, r.Headshots)).ToList();
        return PagedResult<WeaponKillerRow>.Create(items, total, page, pageSize);
    }

    public async Task<(int TotalKills, int TotalHeadshots)> GetWeaponKillTotalsAsync(string code, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var totals = await db.EventFrags
            .Where(f => f.Weapon == code && f.Server!.Game == game)
            .GroupBy(_ => 1)
            .Select(g => new { Kills = g.Count(), Headshots = g.Sum(f => f.Headshot ? 1 : 0) })
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
