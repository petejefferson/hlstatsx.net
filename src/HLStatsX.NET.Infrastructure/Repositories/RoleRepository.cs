using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public RoleRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Role?> GetByCodeAsync(string code, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Roles.FirstOrDefaultAsync(r => r.Code == code && r.Game == game, ct);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Roles
            .Where(r => r.Game == game && r.Kills > 0 && r.Hidden == "0")
            .ToListAsync(ct);
    }

    public async Task<(int TotalKills, int TotalDeaths, int TotalPicked)> GetTotalsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var totals = await db.Roles
            .Where(r => r.Game == game && r.Hidden == "0")
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Kills  = (int?)g.Sum(r => r.Kills),
                Deaths = (int?)g.Sum(r => r.Deaths),
                Picked = (int?)g.Sum(r => r.Picked)
            })
            .FirstOrDefaultAsync(ct);
        return (totals?.Kills ?? 0, totals?.Deaths ?? 0, totals?.Picked ?? 0);
    }

    public async Task<PagedResult<RoleKillerRow>> GetRoleKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var aggregated = db.EventFrags
            .Where(f => f.KillerRole == code)
            .Join(db.Players, f => f.KillerId, p => p.PlayerId, (f, p) => new { f, p })
            .Where(x => x.p.Game == game && x.p.HideRanking == 0)
            .GroupBy(x => new { x.f.KillerId, x.p.LastName, x.p.Flag })
            .Select(g => new
            {
                PlayerId   = g.Key.KillerId,
                PlayerName = g.Key.LastName,
                Flag       = g.Key.Flag,
                Frags      = g.Count()
            });

        aggregated = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("player", true)  => aggregated.OrderByDescending(r => r.PlayerName),
            ("player", false) => aggregated.OrderBy(r => r.PlayerName),
            (_,        true)  => aggregated.OrderByDescending(r => r.Frags).ThenBy(r => r.PlayerName),
            (_,        false) => aggregated.OrderBy(r => r.Frags).ThenBy(r => r.PlayerName)
        };

        var total = await aggregated.CountAsync(ct);
        var rows  = await aggregated.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = rows.Select(r => new RoleKillerRow(r.PlayerId, r.PlayerName, r.Flag, r.Frags)).ToList();
        return PagedResult<RoleKillerRow>.Create(items, total, page, pageSize);
    }

    public async Task<(int TotalKills, int TotalHeadshots)> GetRoleKillTotalsAsync(string code, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var totals = await db.EventFrags
            .Where(f => f.KillerRole == code && f.Server!.Game == game)
            .GroupBy(_ => 1)
            .Select(g => new { Kills = g.Count(), Headshots = g.Sum(f => f.Headshot ? 1 : 0) })
            .FirstOrDefaultAsync(ct);
        return (totals?.Kills ?? 0, totals?.Headshots ?? 0);
    }
}
