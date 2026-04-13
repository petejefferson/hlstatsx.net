using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface ICountryService
{
    Task<PagedResult<CountryLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
    Task<CountryProfile?> GetProfileAsync(string flag, string game, CancellationToken ct = default);
    Task<PagedResult<CountryMember>> GetMembersAsync(string flag, string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default);
}
