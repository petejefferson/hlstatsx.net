using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public ServerRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Server?> GetByIdAsync(int serverId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers
            .SingleOrDefaultAsync(s => s.ServerId == serverId, ct);
    }

    public async Task<IReadOnlyList<Server>> GetAllAsync(string? game = null, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers
            .Where(s => game == null || s.Game == game)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Server>> GetActiveAsync(string? game = null, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers
            .Where(s => s.LastEvent > 0 && (game == null || s.Game == game))
            .OrderByDescending(s => s.ActPlayers)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Livestats
            .Where(l => l.ServerId == serverId)
            .OrderByDescending(l => l.Kills)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Livestats
            .Where(l => l.Server != null && l.Server.Game == game)
            .OrderBy(l => l.Team)
            .ThenByDescending(l => l.Kills)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Teams.Where(t => t.Game == game).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int hours, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();
        return await db.ServerLoads
            .Where(s => s.Server != null && s.Server.Game == game && s.Timestamp >= cutoff)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServerLoad>> GetServerLoadByServerIdAsync(int serverId, int hours, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();
        return await db.ServerLoads
            .Where(s => s.ServerId == serverId && s.Timestamp >= cutoff)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServerLoad>> GetServerLoadAllByServerIdAsync(int serverId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.ServerLoads
            .Where(s => s.ServerId == serverId)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Trend>> GetTrendSeriesAsync(string game, int hours, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();
        return await db.Trends
            .Where(t => t.Game == game && t.Timestamp >= cutoff)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.VoiceCommServers.OrderBy(v => v.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OnlinePlayerLocation>> GetOnlinePlayerLocationsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var rows = await (
            from ls in db.Livestats
            join p in db.Players on ls.PlayerId equals p.PlayerId
            join s in db.Servers on ls.ServerId equals s.ServerId
            where s.Game == game
            where p.Lat.HasValue && p.Lat != 0 && p.Lng.HasValue && p.Lng != 0
            select new { p.PlayerId, p.LastName, p.Country, p.City, p.Lat, p.Lng, p.Kills, p.Deaths }
        ).ToListAsync(ct);

        return rows.Select(r => new OnlinePlayerLocation(
            r.PlayerId, r.LastName, r.Country, r.City,
            r.Lat!.Value, r.Lng!.Value, r.Kills, r.Deaths
        )).ToList();
    }

    public async Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // kills/headshots/server count from hlstats_Servers (matches PHP)
        var servers = await db.Servers.Where(s => s.Game == game).ToListAsync(ct);
        var totalKills      = servers.Sum(s => (long)s.Kills);
        var totalHeadshots  = servers.Sum(s => (long)s.Headshots);
        var serverCount     = servers.Count;

        // 24 h ago snapshot from hlstats_Trend
        var cutoff = (int)DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeSeconds();
        var trend = await db.Trends
            .Where(t => t.Game == game && t.Timestamp <= cutoff)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync(ct);

        return new GameStats(totalKills, totalHeadshots, serverCount,
            trend?.Players ?? -1, trend?.Kills >= 0 ? (long)trend.Kills : -1L);
    }

    public async Task UpdateAsync(Server server, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Servers.Update(server);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddAsync(Server server, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.Servers.Add(server);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int serverId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var server = await db.Servers.FindAsync(new object[] { serverId }, ct);
        if (server is not null)
        {
            db.Servers.Remove(server);
            await db.SaveChangesAsync(ct);
        }
    }
}
