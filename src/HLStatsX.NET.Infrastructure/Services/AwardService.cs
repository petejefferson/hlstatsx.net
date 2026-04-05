using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;

namespace HLStatsX.NET.Infrastructure.Services;

public class AwardService : IAwardService
{
    private readonly IAwardRepository _awards;
    private readonly IPlayerRepository _players;

    public AwardService(IAwardRepository awards, IPlayerRepository players)
    {
        _awards = awards;
        _players = players;
    }

    public Task<IReadOnlyList<Award>> GetAwardsAsync(string game, CancellationToken ct = default) =>
        _awards.GetAllAsync(game, ct);

    public Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default) =>
        _awards.GetDailyAwardsAsync(game, ct);

    public Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default) =>
        _awards.GetRanksAsync(game, ct);

    public async Task<Rank?> GetRankForPlayerAsync(int playerId, string game, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct);
        if (player is null) return null;
        return await _awards.GetRankForKillsAsync(game, player.Kills, ct);
    }

    public Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default) =>
        _awards.GetRibbonsAsync(game, ct);

    public Task<Ribbon?> GetRibbonAsync(int ribbonId, CancellationToken ct = default) =>
        _awards.GetRibbonByIdAsync(ribbonId, ct);
}
