using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for all clan-facing pages: leaderboard, profile, and member lookup.
/// All profile-tab methods map directly to the equivalent <see cref="IClanRepository"/> calls.
/// </summary>
public interface IClanService
{
    /// <summary>Returns a clan by primary key, or <c>null</c> if not found.</summary>
    Task<Clan?> GetClanAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns a paged, sorted clan leaderboard filtered by <paramref name="minMembers"/>.</summary>
    Task<PagedResult<ClanLeaderboardRow>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool desc = true, int minMembers = 3, CancellationToken ct = default);

    /// <summary>Returns the total number of clans in the leaderboard (used for pagination headers).</summary>
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all players who are members of the given clan.</summary>
    Task<IReadOnlyList<Player>> GetMembersAsync(int clanId, CancellationToken ct = default);

    /// <summary>Searches clan names for the given query string.</summary>
    Task<PagedResult<Clan>> SearchClansAsync(string query, string game, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns all clans from the given country (used on the country profile page).</summary>
    Task<IReadOnlyList<Clan>> GetClansByCountryAsync(string countryCode, string game, CancellationToken ct = default);

    // Profile tab data

    /// <summary>Returns aggregate kill/death/skill stats for the clan summary card.</summary>
    Task<ClanSummaryStats?> GetSummaryAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns the server where the clan has played the most time.</summary>
    Task<ClanFavoriteServer?> GetFavoriteServerAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns the map name where the clan has the most kills.</summary>
    Task<string?> GetFavoriteMapAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns the weapon most used by the clan's members.</summary>
    Task<ClanFavoriteWeapon?> GetFavoriteWeaponAsync(int clanId, string game, CancellationToken ct = default);

    /// <summary>Returns a sorted, paged list of clan members with per-player kill stats.</summary>
    Task<PagedResult<ClanMemberRow>> GetMembersPagedAsync(int clanId, string game, int page, int pageSize, string sortBy, bool desc, long totalClanKills, CancellationToken ct = default);

    /// <summary>Returns weapon kill counts for the clan, with headshot percentages against clan-wide totals.</summary>
    Task<IReadOnlyList<ClanWeaponRow>> GetWeaponUsageAsync(int clanId, string game, long realKills, long realHeadshots, CancellationToken ct = default);

    /// <summary>Returns per-map kill/headshot stats for the clan profile.</summary>
    Task<IReadOnlyList<ClanMapRow>> GetMapPerformanceAsync(int clanId, long realKills, long realHeadshots, CancellationToken ct = default);

    /// <summary>Returns game actions performed by the clan.</summary>
    Task<IReadOnlyList<ClanActionRow>> GetActionsAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns game actions where the clan was the victim.</summary>
    Task<IReadOnlyList<ClanActionRow>> GetActionVictimsAsync(int clanId, CancellationToken ct = default);

    /// <summary>Returns team selection breakdown for the clan's members.</summary>
    Task<IReadOnlyList<ClanTeamRow>> GetTeamSelectionAsync(int clanId, string game, CancellationToken ct = default);

    /// <summary>Returns role/class selection breakdown for the clan's members.</summary>
    Task<IReadOnlyList<ClanRoleRow>> GetRoleSelectionAsync(int clanId, string game, CancellationToken ct = default);

    /// <summary>Returns per-weapon shot/hit accuracy stats aggregated across the clan.</summary>
    Task<IReadOnlyList<ClanWeaponStatsRow>> GetWeaponStatsAsync(int clanId, string game, CancellationToken ct = default);

    /// <summary>Returns which player classes/roles the clan kills most frequently.</summary>
    Task<IReadOnlyList<ClanWeaponTargetRow>> GetWeaponTargetsAsync(int clanId, string game, CancellationToken ct = default);

    /// <summary>Returns GeoIP country data for each clan member (used to render the flag cluster on the profile).</summary>
    Task<IReadOnlyList<ClanMemberLocationRow>> GetMemberLocationsAsync(int clanId, CancellationToken ct = default);
}
