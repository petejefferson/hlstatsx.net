using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly HLStatsDbContext _db;

    public EventRepository(HLStatsDbContext db) => _db = db;

    public async Task<PagedResult<EventFrag>> GetFragsAsync(int? playerId = null, int? serverId = null,
        string? game = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.EventFrags
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
        var query = _db.EventChats
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

    public async Task<IReadOnlyList<EventFrag>> GetRecentKillsAsync(int playerId, int count = 20, CancellationToken ct = default) =>
        await _db.EventFrags
            .Include(e => e.Victim)
            .Include(e => e.Killer)
            .Where(e => e.KillerId == playerId)
            .OrderByDescending(e => e.EventTime)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EventFrag>> GetTopVictimsAsync(int killerId, int count = 10, CancellationToken ct = default) =>
        await _db.EventFrags
            .Include(e => e.Victim)
            .Where(e => e.KillerId == killerId)
            .GroupBy(e => e.VictimId)
            .Select(g => g.OrderByDescending(e => e.EventTime).First())
            .OrderByDescending(e => _db.EventFrags.Count(f => f.KillerId == killerId && f.VictimId == e.VictimId))
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EventFrag>> GetTopKillersOfPlayerAsync(int victimId, int count = 10, CancellationToken ct = default) =>
        await _db.EventFrags
            .Include(e => e.Killer)
            .Where(e => e.VictimId == victimId)
            .GroupBy(e => e.KillerId)
            .Select(g => g.OrderByDescending(e => e.EventTime).First())
            .OrderByDescending(e => _db.EventFrags.Count(f => f.VictimId == victimId && f.KillerId == e.KillerId))
            .Take(count)
            .ToListAsync(ct);

    public async Task AddFragAsync(EventFrag frag, CancellationToken ct = default)
    {
        _db.EventFrags.Add(frag);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddChatAsync(EventChat chat, CancellationToken ct = default)
    {
        _db.EventChats.Add(chat);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddConnectAsync(EventConnect connect, CancellationToken ct = default)
    {
        _db.EventConnects.Add(connect);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddDisconnectAsync(EventDisconnect disconnect, CancellationToken ct = default)
    {
        _db.EventDisconnects.Add(disconnect);
        await _db.SaveChangesAsync(ct);
    }
}
