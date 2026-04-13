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

    public async Task<PagedResult<ClanLeaderboardRow>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var projected = db.Clans
            .Where(c => c.Game == game && !c.IsHidden)
            .Select(c => new
            {
                c.ClanId,
                c.Name,
                c.Tag,
                MemberCount   = c.Players.Count(p => p.HideRanking == 0),
                TotalKills    = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.Kills) ?? 0,
                TotalDeaths   = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.Deaths) ?? 0,
                TotalConnTime = c.Players.Where(p => p.HideRanking == 0).Sum(p => (int?)p.ConnectionTime) ?? 0,
                AvgSkill      = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.Skill) ?? 0.0,
                AvgActivity   = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.ActivityScore) ?? 0.0,
            })
            .Where(x => x.MemberCount >= minMembers && x.AvgActivity >= 0);

        var sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("members",    true)  => projected.OrderByDescending(x => x.MemberCount).ThenBy(x => x.Name),
            ("members",    false) => projected.OrderBy(x => x.MemberCount).ThenBy(x => x.Name),
            ("name",       true)  => projected.OrderByDescending(x => x.Name),
            ("name",       false) => projected.OrderBy(x => x.Name),
            ("kills",      true)  => projected.OrderByDescending(x => x.TotalKills).ThenBy(x => x.Name),
            ("kills",      false) => projected.OrderBy(x => x.TotalKills).ThenBy(x => x.Name),
            ("deaths",     true)  => projected.OrderByDescending(x => x.TotalDeaths).ThenBy(x => x.Name),
            ("deaths",     false) => projected.OrderBy(x => x.TotalDeaths).ThenBy(x => x.Name),
            ("skill",      false) => projected.OrderBy(x => x.AvgSkill).ThenBy(x => x.Name),
            _                     => projected.OrderByDescending(x => x.AvgSkill).ThenBy(x => x.Name),
        };

        var total = await sorted.CountAsync(ct);
        var rawItems = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = rawItems.Select(x => new ClanLeaderboardRow
        {
            ClanId              = x.ClanId,
            Name                = x.Name,
            Tag                 = x.Tag,
            MemberCount         = x.MemberCount,
            TotalKills          = x.TotalKills,
            TotalDeaths         = x.TotalDeaths,
            TotalConnectionTime = x.TotalConnTime,
            AvgSkill            = (int)Math.Round(x.AvgSkill),
            AvgActivity         = Math.Round(x.AvgActivity, 2),
        }).ToList();

        return PagedResult<ClanLeaderboardRow>.Create(items, total, page, pageSize);
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
