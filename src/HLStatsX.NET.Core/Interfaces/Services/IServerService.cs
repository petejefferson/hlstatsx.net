using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IServerService
{
    Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default);
    Task<Server?> GetServerAsync(int serverId, CancellationToken ct = default);
    Task<IReadOnlyList<Server>> GetServersAsync(string? game = null, CancellationToken ct = default);
    Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default);
    Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default);
    Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int entries, CancellationToken ct = default);
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);
    Task<Server> CreateServerAsync(Server server, CancellationToken ct = default);
    Task UpdateServerAsync(Server server, CancellationToken ct = default);
    Task DeleteServerAsync(int serverId, CancellationToken ct = default);
}
