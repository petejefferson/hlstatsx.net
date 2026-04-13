using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Infrastructure.Services;

public class ServerService : IServerService
{
    private readonly IServerRepository _servers;
    private readonly IMemoryCache _cache;

    // Teams are static game configuration — cache for 1 hour.
    private static readonly TimeSpan TeamCacheTtl = TimeSpan.FromHours(1);

    public ServerService(IServerRepository servers, IMemoryCache cache)
    {
        _servers = servers;
        _cache   = cache;
    }

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

    public Task<IReadOnlyList<Trend>> GetTrendSeriesAsync(string game, int hours, CancellationToken ct = default) =>
        _servers.GetTrendSeriesAsync(game, hours, ct);

    /// <summary>Teams are static config — served from cache after the first load per game.</summary>
    public async Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default)
    {
        var key = $"teams:{game}";
        if (_cache.TryGetValue(key, out IReadOnlyList<Team>? cached))
            return cached!;

        var teams = await _servers.GetTeamsAsync(game, ct);
        _cache.Set(key, teams, TeamCacheTtl);
        return teams;
    }

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
