using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Handles profile-stat queries for a single player — separated from the core
/// IPlayerRepository so that each class stays focused and testable on its own.
/// </summary>
public interface IPlayerStatsRepository
{
    Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default);
    Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default);
    Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default);
    Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default);
    Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default);
    Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default);
    Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default);
    Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<ServerStatRow>> GetServerPerformanceAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<WeaponStatRow>> GetWeaponStatsAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<TeamStatRow>> GetTeamSelectionAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<ActionStatRow>> GetPlayerActionVictimsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<GlobalAwardRow>> GetGlobalAwardsAsync(int playerId, string game, CancellationToken ct = default);
    Task<int> GetDeleteDaysAsync(CancellationToken ct = default);
}
