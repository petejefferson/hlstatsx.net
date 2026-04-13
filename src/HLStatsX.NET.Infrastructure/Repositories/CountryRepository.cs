using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public CountryRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<PagedResult<CountryLeaderboardRow>> GetRankingsAsync(
        string game, int page, int pageSize,
        string sortBy = "skill", bool desc = true, int minMembers = 3,
        CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // Aggregate player stats by flag (materialized — at most ~200 countries)
        var rawGroups = await db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Flag != null && p.Flag != "")
            .GroupBy(p => p.Flag!)
            .Select(g => new
            {
                Flag         = g.Key,
                MemberCount  = g.Count(),
                TotalKills   = g.Sum(p => (long)p.Kills),
                TotalDeaths  = g.Sum(p => (long)p.Deaths),
                TotalConnTime = g.Sum(p => (long)p.ConnectionTime),
                AvgSkill     = g.Average(p => (double)p.Skill),
                AvgActivity  = g.Average(p => (double)p.ActivityScore),
            })
            .Where(x => x.AvgActivity >= 0 && x.MemberCount >= minMembers)
            .ToListAsync(ct);

        // Bulk-fetch country names
        var flagCodes = rawGroups.Select(x => x.Flag).ToList();
        var countryNames = await db.Countries
            .Where(c => flagCodes.Contains(c.Flag))
            .ToDictionaryAsync(c => c.Flag, c => c.Name, StringComparer.OrdinalIgnoreCase, ct);

        var sortedList = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("members",          true)  => rawGroups.OrderByDescending(x => x.MemberCount).ThenBy(x => x.Flag).ToList(),
            ("members",          false) => rawGroups.OrderBy(x => x.MemberCount).ThenBy(x => x.Flag).ToList(),
            ("kills",            true)  => rawGroups.OrderByDescending(x => x.TotalKills).ThenBy(x => x.Flag).ToList(),
            ("kills",            false) => rawGroups.OrderBy(x => x.TotalKills).ThenBy(x => x.Flag).ToList(),
            ("deaths",           true)  => rawGroups.OrderByDescending(x => x.TotalDeaths).ThenBy(x => x.Flag).ToList(),
            ("deaths",           false) => rawGroups.OrderBy(x => x.TotalDeaths).ThenBy(x => x.Flag).ToList(),
            ("kpd",              true)  => rawGroups.OrderByDescending(x => x.TotalDeaths == 0 ? (double)x.TotalKills : (double)x.TotalKills / x.TotalDeaths).ThenBy(x => x.Flag).ToList(),
            ("kpd",              false) => rawGroups.OrderBy(x => x.TotalDeaths == 0 ? (double)x.TotalKills : (double)x.TotalKills / x.TotalDeaths).ThenBy(x => x.Flag).ToList(),
            ("connection_time",  true)  => rawGroups.OrderByDescending(x => x.TotalConnTime).ThenBy(x => x.Flag).ToList(),
            ("connection_time",  false) => rawGroups.OrderBy(x => x.TotalConnTime).ThenBy(x => x.Flag).ToList(),
            ("name",             true)  => rawGroups.OrderByDescending(x => countryNames.GetValueOrDefault(x.Flag, x.Flag)).ToList(),
            ("name",             false) => rawGroups.OrderBy(x => countryNames.GetValueOrDefault(x.Flag, x.Flag)).ToList(),
            ("skill",            false) => rawGroups.OrderBy(x => x.AvgSkill).ThenBy(x => x.Flag).ToList(),
            _                           => rawGroups.OrderByDescending(x => x.AvgSkill).ThenBy(x => x.Flag).ToList(),
        };

        var total = sortedList.Count;
        var items = sortedList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CountryLeaderboardRow
            {
                Flag                = x.Flag,
                Name                = countryNames.GetValueOrDefault(x.Flag, x.Flag),
                MemberCount         = x.MemberCount,
                TotalKills          = x.TotalKills,
                TotalDeaths         = x.TotalDeaths,
                TotalConnectionTime = x.TotalConnTime,
                AvgSkill            = (int)Math.Round(x.AvgSkill),
                AvgActivity         = Math.Truncate(x.AvgActivity * 100) / 100,
            })
            .ToList();

        return PagedResult<CountryLeaderboardRow>.Create(items, total, page, pageSize);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Flag != null && p.Flag != "")
            .Select(p => p.Flag)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<CountryProfile?> GetProfileAsync(string flag, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var countryName = await db.Countries
            .Where(c => c.Flag == flag)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        var stats = await db.Players
            .Where(p => p.Flag == flag && p.Game == game && p.HideRanking == 0 && p.ActivityScore >= 0)
            .GroupBy(p => p.Flag!)
            .Select(g => new
            {
                MemberCount   = g.Count(),
                TotalKills    = g.Sum(p => (long)p.Kills),
                TotalDeaths   = g.Sum(p => (long)p.Deaths),
                TotalConnTime = g.Sum(p => (long)p.ConnectionTime),
                AvgSkill      = g.Average(p => (double)p.Skill),
                AvgActivity   = g.Average(p => (double)p.ActivityScore),
            })
            .FirstOrDefaultAsync(ct);

        if (stats == null) return null;

        return new CountryProfile
        {
            Flag                = flag,
            Name                = countryName ?? flag,
            MemberCount         = stats.MemberCount,
            TotalKills          = stats.TotalKills,
            TotalDeaths         = stats.TotalDeaths,
            TotalConnectionTime = stats.TotalConnTime,
            AvgSkill            = (int)Math.Round(stats.AvgSkill),
            AvgActivity         = Math.Truncate(stats.AvgActivity * 100) / 100,
        };
    }

    public async Task<PagedResult<CountryMember>> GetMembersAsync(
        string flag, string game, int page, int pageSize,
        string sortBy = "skill", bool desc = true,
        CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var baseQuery = db.Players
            .Where(p => p.Flag == flag && p.Game == game && p.HideRanking == 0 && p.ActivityScore >= 0);

        var totalKills = await baseQuery.SumAsync(p => (long)p.Kills, ct);

        var sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("name",            true)  => baseQuery.OrderByDescending(p => p.LastName),
            ("name",            false) => baseQuery.OrderBy(p => p.LastName),
            ("kills",           true)  => baseQuery.OrderByDescending(p => p.Kills).ThenBy(p => p.LastName),
            ("kills",           false) => baseQuery.OrderBy(p => p.Kills).ThenBy(p => p.LastName),
            ("deaths",          true)  => baseQuery.OrderByDescending(p => p.Deaths).ThenBy(p => p.LastName),
            ("deaths",          false) => baseQuery.OrderBy(p => p.Deaths).ThenBy(p => p.LastName),
            ("connection_time", true)  => baseQuery.OrderByDescending(p => p.ConnectionTime).ThenBy(p => p.LastName),
            ("connection_time", false) => baseQuery.OrderBy(p => p.ConnectionTime).ThenBy(p => p.LastName),
            ("kpd",             true)  => baseQuery.OrderByDescending(p => p.Deaths == 0 ? p.Kills : (double)p.Kills / p.Deaths).ThenBy(p => p.LastName),
            ("kpd",             false) => baseQuery.OrderBy(p => p.Deaths == 0 ? p.Kills : (double)p.Kills / p.Deaths).ThenBy(p => p.LastName),
            ("skill",           false) => baseQuery.OrderBy(p => p.Skill).ThenBy(p => p.LastName),
            _                          => baseQuery.OrderByDescending(p => p.Skill).ThenBy(p => p.LastName),
        };

        var total = await sorted.CountAsync(ct);
        var rawItems = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = rawItems.Select(p => new CountryMember
        {
            PlayerId       = p.PlayerId,
            Name           = p.LastName,
            Flag           = p.Flag,
            Country        = p.Country,
            Skill          = p.Skill,
            MmRank         = p.MmRank,
            Activity       = p.ActivityScore,
            ConnectionTime = p.ConnectionTime,
            Kills          = p.Kills,
            Deaths         = p.Deaths,
            KillPercent    = totalKills == 0 ? 0 : Math.Round((double)p.Kills / totalKills * 100, 2),
        }).ToList();

        return PagedResult<CountryMember>.Create(items, total, page, pageSize);
    }
}
