using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IPlayerService
{
    Task<Player?> GetPlayerAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<Player>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool descending = true, int minKills = 1, CancellationToken ct = default);
    Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, CancellationToken ct = default);
    Task<PagedResult<PlayerLeaderboardRow>> GetPeriodLeaderboardAsync(string game, DateTime from, DateTime to, int page, int pageSize, string sortBy, bool descending, int minKills = 1, CancellationToken ct = default);
    Task<int> GetPlayerRankAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerHistory>> GetPlayerHistoryAsync(int playerId, int days = 30, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerAward>> GetPlayerAwardsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerRibbon>> GetPlayerRibbonsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerName>> GetPlayerAliasesAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<PlayerSearchResult>> SearchPlayersAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetBannedPlayersAsync(string game, CancellationToken ct = default);
    Task BanPlayerAsync(int playerId, string reason, CancellationToken ct = default);
    Task UnbanPlayerAsync(int playerId, CancellationToken ct = default);
    Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
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
    Task<IReadOnlyList<TrendPoint>> GetTrendDataAsync(int playerId, int limit = 30, CancellationToken ct = default);
}
