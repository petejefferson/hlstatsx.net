using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IClanService
{
    Task<Clan?> GetClanAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> SearchClansAsync(string query, string game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Clan>> GetClansByCountryAsync(string countryCode, string game, CancellationToken ct = default);
}
