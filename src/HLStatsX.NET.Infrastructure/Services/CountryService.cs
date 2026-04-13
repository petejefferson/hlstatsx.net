using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class CountryService : ICountryService
{
    private readonly ICountryRepository _countries;

    public CountryService(ICountryRepository countries) => _countries = countries;

    public Task<PagedResult<CountryLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default) =>
        _countries.GetRankingsAsync(game, page, pageSize, sortBy, desc, minMembers, ct);

    public Task<int> GetTotalCountAsync(string game, CancellationToken ct = default) =>
        _countries.GetTotalCountAsync(game, ct);

    public Task<CountryProfile?> GetProfileAsync(string flag, string game, CancellationToken ct = default) =>
        _countries.GetProfileAsync(flag, game, ct);

    public Task<PagedResult<CountryMember>> GetMembersAsync(string flag, string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default) =>
        _countries.GetMembersAsync(flag, game, page, pageSize, sortBy, desc, ct);
}
