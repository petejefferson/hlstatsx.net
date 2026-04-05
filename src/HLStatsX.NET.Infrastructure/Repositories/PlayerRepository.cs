using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly HLStatsDbContext _db;

    public PlayerRepository(HLStatsDbContext db) => _db = db;

    public async Task<Player?> GetByIdAsync(int playerId, CancellationToken ct = default) =>
        await _db.Players
            .Include(p => p.Clan)
            .Include(p => p.UniqueIds)
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);

    public async Task<PagedResult<Player>> GetRankingsAsync(string game, int page, int pageSize,
        string sortBy = "skill", bool descending = true, CancellationToken ct = default)
    {
        IQueryable<Player> query = _db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills > 0)
            .Include(p => p.Clan);

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("skill", true) => query.OrderByDescending(p => p.Skill),
            ("kills", true) => query.OrderByDescending(p => p.Kills),
            ("deaths", true) => query.OrderByDescending(p => p.Deaths),
            ("headshots", true) => query.OrderByDescending(p => p.Headshots),
            ("connectiontime", true) => query.OrderByDescending(p => p.ConnectionTime),
            ("skill", false) => query.OrderBy(p => p.Skill),
            _ => query.OrderByDescending(p => p.Skill)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Player>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<PlayerName>> GetAliasesAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerNames
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.Numuses)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerHistory>> GetHistoryAsync(int playerId, int days = 30, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _db.PlayerHistories
            .Where(h => h.PlayerId == playerId && h.EventTime >= cutoff)
            .OrderBy(h => h.EventTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlayerAward>> GetAwardsAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerAwards
            .Include(a => a.Award)
            .Where(a => a.PlayerId == playerId)
            .OrderByDescending(a => a.Count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerRibbon>> GetRibbonsAsync(int playerId, CancellationToken ct = default) =>
        await _db.PlayerRibbons
            .Include(r => r.Ribbon)
            .Where(r => r.PlayerId == playerId)
            .ToListAsync(ct);

    public async Task<int> GetRankAsync(int playerId, string game, CancellationToken ct = default)
    {
        var player = await _db.Players.FindAsync(new object[] { playerId }, ct);
        if (player is null) return 0;

        return await _db.Players
            .Where(p => p.Game == game && p.HideRanking == 0 && p.Kills > 0 && p.Skill > player.Skill)
            .CountAsync(ct) + 1;
    }

    public async Task<PagedResult<Player>> SearchAsync(string query, string game, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Players
            .Where(p => p.Game == game && EF.Functions.Like(p.LastName, $"%{query}%"))
            .Include(p => p.Clan);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.Skill).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<Player>.Create(items, total, page, pageSize);
    }

    // HLStatsX uses hideranking=1 to "ban"/hide players from rankings
    public async Task<IReadOnlyList<Player>> GetBannedAsync(string game, CancellationToken ct = default) =>
        await _db.Players
            .Where(p => p.Game == game && p.HideRanking == 1)
            .OrderByDescending(p => p.LastEvent)
            .ToListAsync(ct);

    public async Task<Player?> GetBySteamIdAsync(string steamId, string game, CancellationToken ct = default) =>
        await _db.PlayerUniqueIds
            .Where(u => u.UniqueId == steamId && u.Game == game)
            .Select(u => u.Player)
            .FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(string game, CancellationToken ct = default) =>
        await _db.Players.CountAsync(p => p.Game == game, ct);

    public async Task<long> GetTotalKillsAsync(string game, CancellationToken ct = default) =>
        await _db.Players.Where(p => p.Game == game).SumAsync(p => (long)p.Kills, ct);
}
