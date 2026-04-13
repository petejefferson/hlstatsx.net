using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class ClanService : IClanService
{
    private readonly IClanRepository _clans;

    public ClanService(IClanRepository clans) => _clans = clans;

    public Task<Clan?> GetClanAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetByIdAsync(clanId, ct);

    public Task<PagedResult<ClanLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default) =>
        _clans.GetRankingsAsync(game, page, pageSize, sortBy, desc, minMembers, ct);

    public Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetMembersAsync(clanId, ct);

    public Task<PagedResult<Clan>> SearchClansAsync(string query, string game, int page, int pageSize, CancellationToken ct = default) =>
        _clans.SearchAsync(query, game, page, pageSize, ct);

    public Task<IReadOnlyList<Clan>> GetClansByCountryAsync(string countryCode, string game, CancellationToken ct = default) =>
        _clans.GetByCountryAsync(countryCode, game, ct);
}
