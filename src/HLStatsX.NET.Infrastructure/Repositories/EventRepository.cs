using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public EventRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<PagedResult<EventFrag>> GetFragsAsync(int? playerId = null, int? serverId = null,
        string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.EventFrags
            .Include(e => e.Killer)
            .Include(e => e.Victim)
            .AsQueryable();

        if (playerId.HasValue)
            query = query.Where(e => e.KillerId == playerId || e.VictimId == playerId);
        if (serverId.HasValue)
            query = query.Where(e => e.ServerId == serverId);
        query = query.OrderByDescending(e => e.EventTime);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<EventFrag>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<EventChat>> GetChatAsync(int? playerId = null, int? serverId = null,
        string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.EventChats
            .Include(e => e.Player)
            .AsQueryable();

        if (playerId.HasValue)
            query = query.Where(e => e.PlayerId == playerId);
        if (serverId.HasValue)
            query = query.Where(e => e.ServerId == serverId);

        query = query.OrderByDescending(e => e.EventTime);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<EventChat>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<EventFrag>> GetRecentKillsAsync(int playerId, int count = 20, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventFrags
            .Include(e => e.Victim)
            .Include(e => e.Killer)
            .Where(e => e.KillerId == playerId)
            .OrderByDescending(e => e.EventTime)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventFrag>> GetTopVictimsAsync(int killerId, int count = 10, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // GroupBy aggregation: one query for kill counts + most-recent frag per victim.
        // Avoids the N+1 correlated subquery that fired per row in the old OrderByDescending.
        var grouped = await db.EventFrags
            .Where(e => e.KillerId == killerId)
            .GroupBy(e => e.VictimId)
            .Select(g => new
            {
                VictimId  = g.Key,
                KillCount = g.Count(),
                LastFragId = g.Max(e => e.Id)
            })
            .OrderByDescending(x => x.KillCount)
            .Take(count)
            .ToListAsync(ct);

        // Load the representative EventFrag rows and their Victim navigations in one query
        var fragIds = grouped.Select(x => x.LastFragId).ToList();
        var frags   = await db.EventFrags
            .Include(e => e.Victim)
            .Where(e => fragIds.Contains(e.Id))
            .ToListAsync(ct);

        // Preserve ordering by kill count
        var fragById = frags.ToDictionary(e => e.Id);
        return grouped
            .Where(x => fragById.ContainsKey(x.LastFragId))
            .Select(x => fragById[x.LastFragId])
            .ToList();
    }

    public async Task<IReadOnlyList<EventFrag>> GetTopKillersOfPlayerAsync(int victimId, int count = 10, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        // GroupBy aggregation: one query for death counts + most-recent frag per killer.
        // Avoids the N+1 correlated subquery that fired per row in the old OrderByDescending.
        var grouped = await db.EventFrags
            .Where(e => e.VictimId == victimId)
            .GroupBy(e => e.KillerId)
            .Select(g => new
            {
                KillerId   = g.Key,
                KillCount  = g.Count(),
                LastFragId = g.Max(e => e.Id)
            })
            .OrderByDescending(x => x.KillCount)
            .Take(count)
            .ToListAsync(ct);

        var fragIds = grouped.Select(x => x.LastFragId).ToList();
        var frags   = await db.EventFrags
            .Include(e => e.Killer)
            .Where(e => fragIds.Contains(e.Id))
            .ToListAsync(ct);

        var fragById = frags.ToDictionary(e => e.Id);
        return grouped
            .Where(x => fragById.ContainsKey(x.LastFragId))
            .Select(x => fragById[x.LastFragId])
            .ToList();
    }

    public async Task AddFragAsync(EventFrag frag, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.EventFrags.Add(frag);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddChatAsync(EventChat chat, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.EventChats.Add(chat);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddConnectAsync(EventConnect connect, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.EventConnects.Add(connect);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddDisconnectAsync(EventDisconnect disconnect, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.EventDisconnects.Add(disconnect);
        await db.SaveChangesAsync(ct);
    }
}
