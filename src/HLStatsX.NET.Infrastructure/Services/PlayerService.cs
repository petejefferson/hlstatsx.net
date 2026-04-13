using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IPlayerStatsRepository _stats;

    public PlayerService(IPlayerRepository players, IPlayerStatsRepository stats)
    {
        _players = players;
        _stats   = stats;
    }

    public Task<Player?> GetPlayerAsync(int playerId, CancellationToken ct = default) =>
        _players.GetByIdAsync(playerId, ct);

    public Task<PagedResult<Player>> GetLeaderboardAsync(string game, int page, int pageSize,
        string sortBy = "skill", bool descending = true, int minKills = 1, CancellationToken ct = default) =>
        _players.GetRankingsAsync(game, page, pageSize, sortBy, descending, minKills, ct);

    public Task<IReadOnlyList<DateTime>> GetHistoryDatesAsync(string game, CancellationToken ct = default) =>
        _players.GetHistoryDatesAsync(game, 50, ct);

    public Task<PagedResult<PlayerLeaderboardRow>> GetPeriodLeaderboardAsync(string game, DateTime from, DateTime to, int page, int pageSize, string sortBy, bool descending, int minKills = 1, CancellationToken ct = default) =>
        _players.GetHistoryRankingsAsync(game, from, to, page, pageSize, sortBy, descending, minKills, ct);

    public Task<int> GetPlayerRankAsync(int playerId, string game, CancellationToken ct = default) =>
        _players.GetRankAsync(playerId, game, ct);

    public Task<IReadOnlyList<PlayerHistory>> GetPlayerHistoryAsync(int playerId, int days = 30, CancellationToken ct = default) =>
        _players.GetHistoryAsync(playerId, days, ct);

    public Task<IReadOnlyList<PlayerAward>> GetPlayerAwardsAsync(int playerId, CancellationToken ct = default) =>
        _players.GetAwardsAsync(playerId, ct);

    public Task<IReadOnlyList<PlayerRibbon>> GetPlayerRibbonsAsync(int playerId, CancellationToken ct = default) =>
        _players.GetRibbonsAsync(playerId, ct);

    public Task<IReadOnlyList<PlayerName>> GetPlayerAliasesAsync(int playerId, CancellationToken ct = default) =>
        _players.GetAliasesAsync(playerId, ct);

    public Task<PagedResult<PlayerSearchResult>> SearchPlayersAsync(string query, string? game, int page, int pageSize, CancellationToken ct = default) =>
        _players.SearchAsync(query, game, page, pageSize, ct);

    public Task<IReadOnlyList<Player>> GetBannedPlayersAsync(string game, CancellationToken ct = default) =>
        _players.GetBannedAsync(game, ct);

    // HLStatsX bans players by setting hideranking = 1
    public async Task BanPlayerAsync(int playerId, string reason, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct)
            ?? throw new KeyNotFoundException($"Player {playerId} not found.");
        player.HideRanking = 1;
        await _players.UpdateAsync(player, ct);
    }

    public Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default) =>
        _players.GetTotalKillsAsync(game, ct);

    public Task<int> GetTotalCountAsync(string game, CancellationToken ct = default) =>
        _players.GetTotalCountAsync(game, ct);

    public async Task UnbanPlayerAsync(int playerId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct)
            ?? throw new KeyNotFoundException($"Player {playerId} not found.");
        player.HideRanking = 0;
        await _players.UpdateAsync(player, ct);
    }

    // — Profile stats — delegated to PlayerStatsRepository via IPlayerStatsRepository

    public Task<RealStats> GetRealStatsAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetRealStatsAsync(playerId, ct);

    public Task<PingStats?> GetAveragePingAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetAveragePingAsync(playerId, ct);

    public Task<DateTime?> GetLastConnectAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetLastConnectAsync(playerId, ct);

    public Task<FavoriteServer?> GetFavoriteServerAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetFavoriteServerAsync(playerId, ct);

    public Task<string?> GetFavoriteMapAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetFavoriteMapAsync(playerId, ct);

    public Task<FavoriteWeapon?> GetFavoriteWeaponAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetFavoriteWeaponAsync(playerId, ct);

    public Task<Rank?> GetNextRankAsync(string game, int kills, CancellationToken ct = default) =>
        _stats.GetNextRankAsync(game, kills, ct);

    public Task<IReadOnlyList<RibbonDisplay>> GetRibbonsWithStatusAsync(int playerId, string game, CancellationToken ct = default) =>
        _stats.GetRibbonsWithStatusAsync(playerId, game, ct);

    public Task<IReadOnlyList<KillStatRow>> GetKillStatsAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetKillStatsAsync(playerId, ct);

    public Task<IReadOnlyList<MapStatRow>> GetMapPerformanceAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetMapPerformanceAsync(playerId, ct);

    public Task<IReadOnlyList<ServerStatRow>> GetServerPerformanceAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetServerPerformanceAsync(playerId, ct);

    public Task<IReadOnlyList<WeaponStatRow>> GetWeaponStatsAsync(int playerId, string game, CancellationToken ct = default) =>
        _stats.GetWeaponStatsAsync(playerId, game, ct);

    public Task<IReadOnlyList<TeamStatRow>> GetTeamSelectionAsync(int playerId, string game, CancellationToken ct = default) =>
        _stats.GetTeamSelectionAsync(playerId, game, ct);

    public Task<IReadOnlyList<RoleStatRow>> GetRoleSelectionAsync(int playerId, string game, CancellationToken ct = default) =>
        _stats.GetRoleSelectionAsync(playerId, game, ct);

    public Task<IReadOnlyList<ActionStatRow>> GetPlayerActionsAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetPlayerActionsAsync(playerId, ct);

    public Task<IReadOnlyList<ActionStatRow>> GetPlayerActionVictimsAsync(int playerId, CancellationToken ct = default) =>
        _stats.GetPlayerActionVictimsAsync(playerId, ct);

    public Task<IReadOnlyList<TrendPoint>> GetTrendDataAsync(int playerId, int limit = 30, CancellationToken ct = default) =>
        _players.GetTrendAsync(playerId, limit, ct);
}
