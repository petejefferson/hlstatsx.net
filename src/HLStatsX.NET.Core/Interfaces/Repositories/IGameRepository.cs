using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default);
}
