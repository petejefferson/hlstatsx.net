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

    public Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int hours, CancellationToken ct = default) =>
        _servers.GetServerLoadAsync(game, hours, ct);

    public Task<IReadOnlyList<ServerLoad>> GetServerLoadByServerIdAsync(int serverId, int hours, CancellationToken ct = default) =>
        _servers.GetServerLoadByServerIdAsync(serverId, hours, ct);

    public async Task<IReadOnlyList<ServerLoad>> GetServerLoadRangedAsync(int serverId, int range, CancellationToken ct = default)
    {
        IReadOnlyList<ServerLoad> rows = range == 1
            ? await _servers.GetServerLoadByServerIdAsync(serverId, 24, ct)
            : await _servers.GetServerLoadAllByServerIdAsync(serverId, ct);
        if (range == 1 || rows.Count == 0) return rows;
        // Fixed chunk sizes per range: range=2 → weekly (7), range=3 → monthly (30), range=4 → yearly (365)
        int avgStep = range switch { 2 => 7, 3 => 30, _ => 365 };
        if (rows.Count <= avgStep) return rows;
        return Downsample(rows, avgStep);
    }

    private static IReadOnlyList<ServerLoad> Downsample(IReadOnlyList<ServerLoad> rows, int avgStep)
    {
        var result = new List<ServerLoad>(rows.Count / avgStep + 1);
        for (int i = 0; i < rows.Count; i += avgStep)
        {
            int end   = Math.Min(i + avgStep, rows.Count);
            int count = end - i;
            int mid   = (int)Math.Ceiling(avgStep / 2.0) - 1;
            if (mid >= count) mid = count - 1;

            double act = 0, min = 0, max = 0;
            for (int j = i; j < end; j++)
            {
                act += rows[j].ActPlayers;
                min += rows[j].MinPlayers;
                max += rows[j].MaxPlayers;
            }
            result.Add(new ServerLoad
            {
                ServerId   = rows[i].ServerId,
                Timestamp  = rows[i + mid].Timestamp,
                ActPlayers = (int)Math.Round(act / count),
                MinPlayers = (int)Math.Round(min / count),
                MaxPlayers = (int)Math.Round(max / count),
                Map        = rows[end - 1].Map
            });
        }
        return result;
    }

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

    public Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default) =>
        _servers.GetVoiceCommServersAsync(ct);

    public Task<IReadOnlyList<OnlinePlayerLocation>> GetOnlinePlayerLocationsAsync(string game, CancellationToken ct = default) =>
        _servers.GetOnlinePlayerLocationsAsync(game, ct);
}
