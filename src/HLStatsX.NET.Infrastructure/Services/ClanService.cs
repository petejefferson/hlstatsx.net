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

    public Task<ClanSummaryStats?> GetSummaryAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetSummaryAsync(clanId, ct);

    public Task<ClanFavoriteServer?> GetFavoriteServerAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetFavoriteServerAsync(clanId, ct);

    public Task<string?> GetFavoriteMapAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetFavoriteMapAsync(clanId, ct);

    public Task<ClanFavoriteWeapon?> GetFavoriteWeaponAsync(int clanId, string game, CancellationToken ct = default) =>
        _clans.GetFavoriteWeaponAsync(clanId, game, ct);

    public Task<PagedResult<ClanMemberRow>> GetMembersPagedAsync(int clanId, int page, int pageSize, string sortBy, bool desc, long totalClanKills, CancellationToken ct = default) =>
        _clans.GetMembersPagedAsync(clanId, page, pageSize, sortBy, desc, totalClanKills, ct);

    public Task<IReadOnlyList<ClanWeaponRow>> GetWeaponUsageAsync(int clanId, string game, long realKills, long realHeadshots, CancellationToken ct = default) =>
        _clans.GetWeaponUsageAsync(clanId, game, realKills, realHeadshots, ct);

    public Task<IReadOnlyList<ClanMapRow>> GetMapPerformanceAsync(int clanId, long realKills, long realHeadshots, CancellationToken ct = default) =>
        _clans.GetMapPerformanceAsync(clanId, realKills, realHeadshots, ct);

    public Task<IReadOnlyList<ClanActionRow>> GetActionsAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetActionsAsync(clanId, ct);

    public Task<IReadOnlyList<ClanActionRow>> GetActionVictimsAsync(int clanId, CancellationToken ct = default) =>
        _clans.GetActionVictimsAsync(clanId, ct);

    public Task<IReadOnlyList<ClanTeamRow>> GetTeamSelectionAsync(int clanId, string game, CancellationToken ct = default) =>
        _clans.GetTeamSelectionAsync(clanId, game, ct);

    public Task<IReadOnlyList<ClanRoleRow>> GetRoleSelectionAsync(int clanId, string game, CancellationToken ct = default) =>
        _clans.GetRoleSelectionAsync(clanId, game, ct);
}
