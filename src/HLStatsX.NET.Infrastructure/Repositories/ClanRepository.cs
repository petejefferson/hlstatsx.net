using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class ClanRepository : IClanRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public ClanRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Clan?> GetByIdAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.FirstOrDefaultAsync(c => c.ClanId == clanId, ct);
    }

    public async Task<PagedResult<Clan>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.Clans
            .Where(c => c.Game == game && !c.IsHidden)
            .Select(c => new
            {
                Clan = c,
                MemberCount = c.Players.Count(p => p.HideRanking == 0 && p.Kills > 0),
                TotalSkill = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.Skill) ?? 0
            });

        query = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("members", true)  => query.OrderByDescending(x => x.MemberCount),
            ("members", false) => query.OrderBy(x => x.MemberCount),
            ("name",    true)  => query.OrderByDescending(x => x.Clan.Name),
            ("name",    false) => query.OrderBy(x => x.Clan.Name),
            ("skill",   false) => query.OrderBy(x => x.TotalSkill),
            _                  => query.OrderByDescending(x => x.TotalSkill)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Clan).ToListAsync(ct);
        return PagedResult<Clan>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players
            .Where(p => p.ClanId == clanId && p.HideRanking == 0)
            .OrderByDescending(p => p.Skill)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Clan>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var q = db.Clans
            .Where(c => (game == null || c.Game == game)
                     && (EF.Functions.Like(c.Name, $"%{query}%") || EF.Functions.Like(c.Tag, $"%{query}%")));

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Clan>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Clan>> GetByCountryAsync(string countryCode, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans
            .Where(c => c.Game == game && c.MapRegion == countryCode && !c.IsHidden)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Clan clan, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Clans.Update(clan);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.CountAsync(c => c.Game == game && !c.IsHidden, ct);
    }
}
