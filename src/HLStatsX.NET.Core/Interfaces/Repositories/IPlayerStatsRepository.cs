using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Handles profile-stat queries for a single player — separated from the core
/// IPlayerRepository so that each class stays focused and testable on its own.
/// </summary>
public interface IPlayerStatsRepository
{
    /// <summary>Returns aggregated kill/death/headshot totals from the raw event tables (not the denormalised player row).</summary>
    Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns average ping and latency from <c>hlstats_Events_Latency</c>, or <c>null</c> if no data.</summary>
    Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the timestamp of the player's most recent connect event, or <c>null</c> if none.</summary>
    Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the server where the player has spent the most time, or <c>null</c> if no sessions.</summary>
    Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the map where the player has scored the most kills, or <c>null</c> if no kills.</summary>
    Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the weapon the player has scored the most kills with, or <c>null</c> if no kills.</summary>
    Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns the next rank the player will achieve, or <c>null</c> if they have reached the top rank.</summary>
    Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default);

    /// <summary>Returns every ribbon defined for the game, annotated with whether the player has earned each one.</summary>
    Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns head-to-head kill/death records against opponents.
    /// When <paramref name="killLimit"/> is greater than zero, only opponents
    /// with at least that many kills against the player are included.
    /// </summary>
    Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, int killLimit = 0, CancellationToken ct = default);

    /// <summary>Returns kill/death/headshot totals grouped by map.</summary>
    Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns kill/death/headshot totals grouped by server.</summary>
    Task<IReadOnlyList<ServerStatRow>> GetServerPerformanceAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns kill and headshot totals per weapon.</summary>
    Task<IReadOnlyList<WeaponStatRow>> GetWeaponStatsAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns detailed accuracy stats per weapon, sourced from the statsme plugin event tables.</summary>
    Task<IReadOnlyList<WeaponStatsmeRow>> GetWeaponStatsmeAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns per-hitbox breakdown per weapon from the statsme2 event tables.</summary>
    Task<IReadOnlyList<WeaponTargetRow>> GetWeaponTargetsAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns how many times the player joined each team.</summary>
    Task<IReadOnlyList<TeamStatRow>> GetTeamSelectionAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns how many times the player played each role, with kill/death totals.</summary>
    Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>Returns in-game action totals (e.g. bomb plants) where the player was the initiator.</summary>
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns in-game action totals where another player performed the action against this player.</summary>
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionVictimsAsync(int playerId, CancellationToken ct = default);

    /// <summary>Returns all global (all-time) awards the player currently holds.</summary>
    Task<IReadOnlyList<GlobalAwardRow>> GetGlobalAwardsAsync(int playerId, string game, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of days after which inactive players are automatically deleted,
    /// read from the <c>delete_player</c> option in <c>hlstats_Options</c>.
    /// </summary>
    Task<int> GetDeleteDaysAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the tracking mode from <c>hlstats_Options</c> (e.g. <c>"Normal"</c>, <c>"NameTrack"</c>, <c>"LAN"</c>).
    /// Defaults to <c>"Normal"</c> if not set.
    /// </summary>
    Task<string> GetModeAsync(CancellationToken ct = default);
}
