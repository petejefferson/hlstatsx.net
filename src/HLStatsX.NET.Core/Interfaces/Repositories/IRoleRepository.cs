using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for player role/class statistics stored in
/// <c>hlstats_Events_Frags</c> (kill counts per role) and <c>hlstats_Roles</c>.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Returns a role by its game-internal code string, or <c>null</c> if not found.</summary>
    Task<Role?> GetByCodeAsync(string code, string game, CancellationToken ct = default);

    /// <summary>Returns all roles defined for the game.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(string game, CancellationToken ct = default);

    /// <summary>
    /// Returns aggregate kill, death, and pick counts across all roles for the game.
    /// Used as the denominator when calculating per-role percentage bars.
    /// </summary>
    Task<(int TotalKills, int TotalDeaths, int TotalPicked)> GetTotalsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a paged leaderboard of players sorted by kill count with a specific role.</summary>
    Task<PagedResult<RoleKillerRow>> GetRoleKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns total kills and headshots for a single role (denominator for per-player share bars).</summary>
    Task<(int TotalKills, int TotalHeadshots)> GetRoleKillTotalsAsync(string code, string game, CancellationToken ct = default);
}
