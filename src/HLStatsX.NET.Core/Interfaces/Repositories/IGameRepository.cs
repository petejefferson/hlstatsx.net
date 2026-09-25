using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for game metadata — game definitions, teams, roles, and actions.
/// This repository is read-heavy; mutations go through <see cref="IAdminRepository"/>.
/// </summary>
public interface IGameRepository
{
    /// <summary>Returns a game by its short code (e.g. <c>"dods"</c>, <c>"tf"</c>), or <c>null</c> if not found.</summary>
    Task<Game?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns all games, including hidden ones.</summary>
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all teams defined for the given game (e.g. Allies/Axis for DoD:S).</summary>
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all player roles/classes defined for the given game.</summary>
    Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all event actions (bomb plant, flag capture, etc.) defined for the given game.</summary>
    Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns all game actions across every game (used by the admin tools page).</summary>
    Task<IReadOnlyList<GameAction>> GetAllActionsAsync(CancellationToken ct = default);
}
