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
    IReadOnlyList<ClanMapRow> MapPerformance,
    IReadOnlyList<ClanActionRow> Actions,
    IReadOnlyList<ClanActionRow> ActionVictims,
    IReadOnlyList<ClanTeamRow> Teams,
    IReadOnlyList<ClanRoleRow> Roles
)
{
    public string Game => Clan.Game;
};
