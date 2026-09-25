using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for country-level leaderboards and country profile pages.
/// </summary>
public interface ICountryService
{
    /// <summary>Returns a paged, sorted country leaderboard filtered by <paramref name="minMembers"/> ranked players.</summary>
    Task<PagedResult<CountryLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);

    /// <summary>Returns the total number of countries in the leaderboard (used for pagination headers).</summary>
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);

    /// <summary>Returns aggregate stats for a country's profile page, or <c>null</c> if not found.</summary>
    Task<CountryProfile?> GetProfileAsync(string flag, string game, CancellationToken ct = default);

    /// <summary>Returns a paged list of players from the given country.</summary>
    Task<PagedResult<CountryMember>> GetMembersAsync(string flag, string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default);

    /// <summary>Returns GeoIP coordinates for all players in the given country who have a known location.</summary>
    Task<IReadOnlyList<CountryMemberLocationRow>> GetMemberLocationsAsync(string flag, string game, CancellationToken ct = default);
}
