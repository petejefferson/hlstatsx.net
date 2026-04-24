using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class ActionRepository : IActionRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public ActionRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<ActionListRow>> GetListAsync(string game, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var base1 = db.GameActions.Where(a => a.Game == game && a.Count > 0);

        IQueryable<GameAction> sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("description", true)  => base1.OrderByDescending(a => a.Description),
            ("description", false) => base1.OrderBy(a => a.Description),
            ("reward",      true)  => base1.OrderByDescending(a => a.RewardPlayer),
            ("reward",      false) => base1.OrderBy(a => a.RewardPlayer),
            (_,             true)  => base1.OrderByDescending(a => a.Count),
            (_,             false) => base1.OrderBy(a => a.Count)
        };

        return await sorted
            .Select(a => new ActionListRow(a.Code, a.Description ?? a.Code, a.Count, a.RewardPlayer))
            .ToListAsync(ct);
    }

    public async Task<long> GetTotalEarnedAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GameActions
            .Where(a => a.Game == game)
            .SumAsync(a => (long)a.Count, ct);
    }

    public async Task<GameAction?> GetByCodeAsync(string code, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GameActions
            .FirstOrDefaultAsync(a => a.Code == code && a.Game == game, ct);
    }

    public async Task<PagedResult<ActionAchieverRow>> GetAchieversAsync(
        string code, string game, bool usePlayerPlayerActions,
        int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        if (usePlayerPlayerActions)
        {
            var grouped = db.EventPlayerPlayerActions
                .Join(db.Players, e => e.PlayerId, p => p.PlayerId, (e, p) => new { e, p })
                .Join(db.GameActions, ep => ep.e.ActionId, a => a.ActionId, (ep, a) => new { ep.e, ep.p, a })
                .Where(x => x.a.Code == code && x.p.Game == game && x.p.HideRanking == 0)
                .GroupBy(x => new { x.e.PlayerId, x.p.LastName, x.p.Flag, x.a.RewardPlayer })
                .Select(g => new { g.Key.PlayerId, g.Key.LastName, g.Key.Flag, g.Key.RewardPlayer, Count = g.Count() });

            var sorted = (sortBy.ToLowerInvariant(), desc) switch
            {
                ("player", true)  => grouped.OrderByDescending(r => r.LastName),
                ("player", false) => grouped.OrderBy(r => r.LastName),
                (_,        true)  => grouped.OrderByDescending(r => r.Count),
                (_,        false) => grouped.OrderBy(r => r.Count)
            };

            var total = await sorted.CountAsync(ct);
            var raw = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var rows = raw.Select(x => new ActionAchieverRow(x.PlayerId, x.LastName, x.Flag, x.Count, (long)x.Count * x.RewardPlayer)).ToList();
            return PagedResult<ActionAchieverRow>.Create(rows, total, page, pageSize);
        }
        else
        {
            var grouped = db.EventPlayerActions
                .Join(db.Players, e => e.PlayerId, p => p.PlayerId, (e, p) => new { e, p })
                .Join(db.GameActions, ep => ep.e.ActionId, a => a.ActionId, (ep, a) => new { ep.e, ep.p, a })
                .Where(x => x.a.Code == code && x.p.Game == game && x.p.HideRanking == 0)
                .GroupBy(x => new { x.e.PlayerId, x.p.LastName, x.p.Flag, x.a.RewardPlayer })
                .Select(g => new { g.Key.PlayerId, g.Key.LastName, g.Key.Flag, g.Key.RewardPlayer, Count = g.Count() });

            var sorted = (sortBy.ToLowerInvariant(), desc) switch
            {
                ("player", true)  => grouped.OrderByDescending(r => r.LastName),
                ("player", false) => grouped.OrderBy(r => r.LastName),
                (_,        true)  => grouped.OrderByDescending(r => r.Count),
                (_,        false) => grouped.OrderBy(r => r.Count)
            };

            var total = await sorted.CountAsync(ct);
            var raw = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var rows = raw.Select(x => new ActionAchieverRow(x.PlayerId, x.LastName, x.Flag, x.Count, (long)x.Count * x.RewardPlayer)).ToList();
            return PagedResult<ActionAchieverRow>.Create(rows, total, page, pageSize);
        }
    }

    public async Task<long> GetTotalAchievementsAsync(string code, string game, bool usePlayerPlayerActions, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        if (usePlayerPlayerActions)
        {
            return await db.EventPlayerPlayerActions
                .Join(db.Players, e => e.PlayerId, p => p.PlayerId, (e, p) => new { e, p })
                .Join(db.GameActions, ep => ep.e.ActionId, a => a.ActionId, (ep, a) => new { ep.p, a })
                .Where(x => x.a.Code == code && x.p.Game == game && x.p.HideRanking == 0)
                .LongCountAsync(ct);
        }
        else
        {
            return await db.EventPlayerActions
                .Join(db.Players, e => e.PlayerId, p => p.PlayerId, (e, p) => new { e, p })
                .Join(db.GameActions, ep => ep.e.ActionId, a => a.ActionId, (ep, a) => new { ep.p, a })
                .Where(x => x.a.Code == code && x.p.Game == game && x.p.HideRanking == 0)
                .LongCountAsync(ct);
        }
    }

    public async Task<PagedResult<ActionVictimRow>> GetVictimsAsync(
        string code, string game,
        int page, int pageSize, string sortBy, bool desc, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var grouped = db.EventPlayerPlayerActions
            .Join(db.Players, e => e.VictimId, p => p.PlayerId, (e, p) => new { e, p })
            .Join(db.GameActions, ep => ep.e.ActionId, a => a.ActionId, (ep, a) => new { ep.e, ep.p, a })
            .Where(x => x.a.Code == code && x.p.Game == game && x.p.HideRanking == 0)
            .GroupBy(x => new { x.e.VictimId, x.p.LastName, x.p.Flag, x.a.RewardPlayer })
            .Select(g => new { g.Key.VictimId, g.Key.LastName, g.Key.Flag, g.Key.RewardPlayer, Count = g.Count() });

        var sorted = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("player", true)  => grouped.OrderByDescending(r => r.LastName),
            ("player", false) => grouped.OrderBy(r => r.LastName),
            (_,        true)  => grouped.OrderByDescending(r => r.Count),
            (_,        false) => grouped.OrderBy(r => r.Count)
        };

        var total = await sorted.CountAsync(ct);
        var raw = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var rows = raw.Select(x => new ActionVictimRow(x.VictimId, x.LastName, x.Flag, x.Count, -(long)x.Count * x.RewardPlayer)).ToList();
        return PagedResult<ActionVictimRow>.Create(rows, total, page, pageSize);
    }
}
