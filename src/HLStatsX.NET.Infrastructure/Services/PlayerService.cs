using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;

    public PlayerService(IPlayerRepository players) => _players = players;

    public Task<Player?> GetPlayerAsync(int playerId, CancellationToken ct = default) =>
        _players.GetByIdAsync(playerId, ct);

    public Task<PagedResult<Player>> GetLeaderboardAsync(string game, int page, int pageSize,
        string sortBy = "skill", bool descending = true, CancellationToken ct = default) =>
        _players.GetRankingsAsync(game, page, pageSize, sortBy, descending, ct);

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

    public Task<PagedResult<Player>> SearchPlayersAsync(string query, string game, int page, int pageSize, CancellationToken ct = default) =>
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
}
