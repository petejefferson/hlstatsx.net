using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IClanRepository
{
    Task<Clan?> GetByIdAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<ClanLeaderboardRow>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Clan>> GetByCountryAsync(string countryCode, string game, CancellationToken ct = default);
    Task UpdateAsync(Clan clan, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);

    // Profile tab data
    Task<ClanSummaryStats?> GetSummaryAsync(int clanId, CancellationToken ct = default);
    Task<ClanFavoriteServer?> GetFavoriteServerAsync(int clanId, CancellationToken ct = default);
    Task<string?> GetFavoriteMapAsync(int clanId, CancellationToken ct = default);
    Task<ClanFavoriteWeapon?> GetFavoriteWeaponAsync(int clanId, string game, CancellationToken ct = default);
    Task<PagedResult<ClanMemberRow>> GetMembersPagedAsync(int clanId, string game, int page, int pageSize, string sortBy, bool desc, long totalClanKills, CancellationToken ct = default);
    Task<IReadOnlyList<ClanWeaponRow>> GetWeaponUsageAsync(int clanId, string game, long realKills, long realHeadshots, CancellationToken ct = default);
    Task<IReadOnlyList<ClanMapRow>> GetMapPerformanceAsync(int clanId, long realKills, long realHeadshots, CancellationToken ct = default);
    Task<IReadOnlyList<ClanActionRow>> GetActionsAsync(int clanId, CancellationToken ct = default);
    Task<IReadOnlyList<ClanActionRow>> GetActionVictimsAsync(int clanId, CancellationToken ct = default);
    Task<IReadOnlyList<ClanTeamRow>> GetTeamSelectionAsync(int clanId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<ClanRoleRow>> GetRoleSelectionAsync(int clanId, string game, CancellationToken ct = default);
}
