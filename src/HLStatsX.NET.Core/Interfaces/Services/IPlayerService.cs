using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IPlayerService
{
    Task<Player?> GetPlayerAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<Player>> GetLeaderboardAsync(string game, int page, int pageSize, string sortBy = "skill", bool descending = true, CancellationToken ct = default);
    Task<int> GetPlayerRankAsync(int playerId, string game, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerHistory>> GetPlayerHistoryAsync(int playerId, int days = 30, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerAward>> GetPlayerAwardsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerRibbon>> GetPlayerRibbonsAsync(int playerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerName>> GetPlayerAliasesAsync(int playerId, CancellationToken ct = default);
    Task<PagedResult<Player>> SearchPlayersAsync(string query, string game, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetBannedPlayersAsync(string game, CancellationToken ct = default);
    Task BanPlayerAsync(int playerId, string reason, CancellationToken ct = default);
    Task UnbanPlayerAsync(int playerId, CancellationToken ct = default);
    Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string game, CancellationToken ct = default);
}
