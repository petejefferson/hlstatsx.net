using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Core player data access: CRUD, leaderboard, search, history, and basic profile lists.
/// Profile-stat queries (kills breakdown, weapon usage, etc.) live in IPlayerStatsRepository.
/// </summary>
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
}
