using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ClanLeaderboardViewModel(
    PagedResult<ClanLeaderboardRow> Clans,
    string Game,
    string SortBy,
    bool Descending,
    int MinMembers,
    int TotalClans
);

public record ClanProfileViewModel(
    Clan Clan,
    ClanSummaryStats Summary,
    ClanFavoriteServer? FavoriteServer,
    string? FavoriteMap,
    ClanFavoriteWeapon? FavoriteWeapon,
    PagedResult<ClanMemberRow> Members,
    int MembersPage,
    string MembersSortBy,
    bool MembersDescending,
    IReadOnlyList<ClanWeaponRow> WeaponUsage,
    string WeaponsSortBy,
    bool WeaponsDescending,
    IReadOnlyList<ClanWeaponStatsRow> WeaponStats,
    string WeaponStatsSortBy,
    bool WeaponStatsDescending,
    IReadOnlyList<ClanWeaponTargetRow> WeaponTargets,
    string WeaponTargetsSortBy,
    bool WeaponTargetsDescending,
    IReadOnlyList<ClanMapRow> MapPerformance,
    string MapsSortBy,
    bool MapsDescending,
    IReadOnlyList<ClanActionRow> Actions,
    string ActionsSortBy,
    bool ActionsDescending,
    IReadOnlyList<ClanActionRow> ActionVictims,
    string VictimsSortBy,
    bool VictimsDescending,
    IReadOnlyList<ClanTeamRow> Teams,
    string TeamsSortBy,
    bool TeamsDescending,
    IReadOnlyList<ClanRoleRow> Roles,
    string RolesSortBy,
    bool RolesDescending,
    IReadOnlyList<ClanMemberLocationRow> MemberLocations,
    string? GoogleMapsApiKey
)
{
    public string Game => Clan.Game;
};
