using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for country-level leaderboards and profile pages.
/// Player country data is derived from GeoIP lookups performed by the stats daemon at connect time.
/// </summary>
public interface ICountryRepository
{
    /// <summary>Returns a paged, sorted leaderboard of countries with at least <paramref name="minMembers"/> ranked players.</summary>
    Task<PagedResult<CountryLeaderboardRow>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);

    /// <summary>Returns the total number of countries in the leaderboard (used for pagination).</summary>
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);

    /// <summary>Returns aggregate stats for a country's profile page, or <c>null</c> if the flag is not found.</summary>
    Task<CountryProfile?> GetProfileAsync(string flag, string game, CancellationToken ct = default);

    /// <summary>Returns a paged list of players from the given country, sorted by the given column.</summary>
    Task<PagedResult<CountryMember>> GetMembersAsync(string flag, string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default);

    /// <summary>Returns GeoIP coordinates for all players in the given country who have a known location.</summary>
    Task<IReadOnlyList<CountryMemberLocationRow>> GetMemberLocationsAsync(string flag, string game, CancellationToken ct = default);
}
