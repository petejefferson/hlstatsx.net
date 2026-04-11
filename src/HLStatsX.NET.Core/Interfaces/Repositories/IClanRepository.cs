using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IClanRepository
{
    Task<Clan?> GetByIdAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Clan>> GetByCountryAsync(string countryCode, string game, CancellationToken ct = default);
    Task UpdateAsync(Clan clan, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
}
