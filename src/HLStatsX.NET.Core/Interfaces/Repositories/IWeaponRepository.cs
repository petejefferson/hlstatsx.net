using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for weapon statistics stored in <c>hlstats_Weapons</c>
/// and kill counts aggregated from <c>hlstats_Events_Frags</c>.
/// </summary>
public interface IWeaponRepository
{
    /// <summary>Returns a weapon by primary key, or <c>null</c> if not found.</summary>
    Task<Weapon?> GetByIdAsync(int weaponId, CancellationToken ct = default);

    /// <summary>Returns a weapon by its game-internal code string, or <c>null</c> if not found.</summary>
    Task<Weapon?> GetByCodeAsync(string code, string game, CancellationToken ct = default);

    /// <summary>Returns a paged, sortable list of all weapons recorded for the game.</summary>
    Task<PagedResult<Weapon>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default);

    /// <summary>Returns the top <paramref name="count"/> weapons by kill count (used for home page highlights).</summary>
    Task<IReadOnlyList<Weapon>> GetTopWeaponsAsync(string game, int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Returns total kills and headshots across all weapons for the game.
    /// Used to calculate per-weapon percentage bars on the leaderboard.
    /// </summary>
    Task<(int TotalKills, int TotalHeadshots)> GetKillTotalsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a paged leaderboard of players sorted by kill count with a specific weapon.</summary>
    Task<PagedResult<WeaponKillerRow>> GetWeaponKillersAsync(string code, string game, int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default);

    /// <summary>Returns total kills and headshots for a single weapon (denominator for per-player share bars).</summary>
    Task<(int TotalKills, int TotalHeadshots)> GetWeaponKillTotalsAsync(string code, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns all weapons across all games — used to populate the admin help text
    /// that shows which weapon codes are already registered.
    /// </summary>
    Task<IReadOnlyList<Weapon>> GetAllForHelpAsync(CancellationToken ct = default);

    /// <summary>Persists display-name and modifier changes to an existing weapon record.</summary>
    Task UpdateAsync(Weapon weapon, CancellationToken ct = default);
}
