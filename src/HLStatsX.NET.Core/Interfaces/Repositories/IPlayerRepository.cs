using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<Player>> GetRankingsAsync(string game, int page, int pageSize, string sortBy = "skill", bool descending = true, CancellationToken ct = default);
    Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, int count = 50, CancellationToken ct = default);
    Task<PagedResult<PlayerLeaderboardRow>> GetHistoryRankingsAsync(string game, DateTime from, DateTime to, int page, int pageSize, string sortBy = "kills", bool descending = true, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerName>> GetAliasesAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerHistory>> GetHistoryAsync(int playerId, int days = 30, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerAward>> GetAwardsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerRibbon>> GetRibbonsAsync(int playerId, CancellationToken ct = default);
    Task<int> GetRankAsync(int playerId, string game, CancellationToken ct = default);
    Task<PagedResult<PlayerSearchResult>> SearchAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetBannedAsync(string game, CancellationToken ct = default);
    Task<Player?> GetBySteamIdAsync(string steamId, string game, CancellationToken ct = default);
    Task UpdateAsync(Player player, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
    Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default);
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
}
