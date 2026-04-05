using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Infrastructure.Services;

public class ServerService : IServerService
{
    private readonly IServerRepository _servers;

    public ServerService(IServerRepository servers) => _servers = servers;

    public Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default) =>
        _servers.GetGameStatsAsync(game, ct);

    public Task<Server?> GetServerAsync(int serverId, CancellationToken ct = default) =>
        _servers.GetByIdAsync(serverId, ct);

    public Task<IReadOnlyList<Server>> GetServersAsync(string? game = null, CancellationToken ct = default) =>
        _servers.GetActiveAsync(game, ct);

    public Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default) =>
        _servers.GetLivestatsAsync(serverId, ct);

    public Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default) =>
        _servers.GetAllLivestatsAsync(game, ct);

    public Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int entries, CancellationToken ct = default) =>
        _servers.GetServerLoadAsync(game, entries, ct);

    public Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default) =>
        _servers.GetTeamsAsync(game, ct);

    public async Task<Server> CreateServerAsync(Server server, CancellationToken ct = default)
    {
        await _servers.AddAsync(server, ct);
        return server;
    }

    public Task UpdateServerAsync(Server server, CancellationToken ct = default) =>
        _servers.UpdateAsync(server, ct);

    public Task DeleteServerAsync(int serverId, CancellationToken ct = default) =>
        _servers.DeleteAsync(serverId, ct);
}
