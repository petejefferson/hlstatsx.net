using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IServerRepository
{
    Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default);
    Task<Server?> GetByIdAsync(int serverId, CancellationToken ct = default);
    Task<IReadOnlyList<Server>> GetAllAsync(string? game = null, CancellationToken ct = default);
    Task<IReadOnlyList<Server>> GetActiveAsync(string? game = null, CancellationToken ct = default);
    Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default);
    Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int entries, CancellationToken ct = default);
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);
    Task UpdateAsync(Server server, CancellationToken ct = default);
    Task AddAsync(Server server, CancellationToken ct = default);
    Task DeleteAsync(int serverId, CancellationToken ct = default);
}
