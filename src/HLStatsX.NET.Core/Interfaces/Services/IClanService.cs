using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IClanService
{
    Task<Clan?> GetClanAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<ClanLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default);
    Task<PagedResult<Clan>> SearchClansAsync(string query, string game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Clan>> GetClansByCountryAsync(string countryCode, string game, CancellationToken ct = default);

    // Profile tab data
    Task<ClanSummaryStats?> GetSummaryAsync(int clanId, CancellationToken ct = default);
    Task<ClanFavoriteServer?> GetFavoriteServerAsync(int clanId, CancellationToken ct = default);
    Task<string?> GetFavoriteMapAsync(int clanId, CancellationToken ct = default);
    Task<ClanFavoriteWeapon?> GetFavoriteWeaponAsync(int clanId, string game, CancellationToken ct = default);
    Task<PagedResult<ClanMemberRow>> GetMembersPagedAsync(int clanId, int page, int pageSize, string sortBy, bool desc, long totalClanKills, CancellationToken ct = default);
    Task<IReadOnlyList<ClanWeaponRow>> GetWeaponUsageAsync(int clanId, string game, long realKills, long realHeadshots, CancellationToken ct = default);
    Task<IReadOnlyList<ClanMapRow>> GetMapPerformanceAsync(int clanId, long realKills, long realHeadshots, CancellationToken ct = default);
    Task<IReadOnlyList<ClanActionRow>> GetActionsAsync(int clanId, CancellationToken ct = default);
    Task<IReadOnlyList<ClanActionRow>> GetActionVictimsAsync(int clanId, CancellationToken ct = default);
    Task<IReadOnlyList<ClanTeamRow>> GetTeamSelectionAsync(int clanId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<ClanRoleRow>> GetRoleSelectionAsync(int clanId, string game, CancellationToken ct = default);
}
