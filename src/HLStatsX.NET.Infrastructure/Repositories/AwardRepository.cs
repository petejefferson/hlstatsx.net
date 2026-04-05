using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class AwardRepository : IAwardRepository
{
    private readonly HLStatsDbContext _db;

    public AwardRepository(HLStatsDbContext db) => _db = db;

    public async Task<Award?> GetByIdAsync(int awardId, CancellationToken ct = default) =>
        await _db.Awards.FindAsync(new object[] { awardId }, ct);

    public async Task<IReadOnlyList<Award>> GetAllAsync(string game, CancellationToken ct = default) =>
        await _db.Awards
            .Where(a => a.Game == game)
            .Include(a => a.DailyWinner)
            .Include(a => a.GlobalWinner)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Award>> GetDailyAwardsAsync(string game, CancellationToken ct = default) =>
        await _db.Awards
            .Where(a => a.Game == game)
            .Include(a => a.DailyWinner)
            .Include(a => a.GlobalWinner)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rank>> GetRanksAsync(string game, CancellationToken ct = default) =>
        await _db.Ranks.Where(r => r.Game == game).OrderBy(r => r.MinKills).ToListAsync(ct);

    public async Task<Rank?> GetRankForKillsAsync(string game, int kills, CancellationToken ct = default) =>
        await _db.Ranks
            .Where(r => r.Game == game && r.MinKills <= kills && (r.MaxKills == 0 || r.MaxKills >= kills))
            .OrderByDescending(r => r.MinKills)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Ribbon>> GetRibbonsAsync(string game, CancellationToken ct = default) =>
        await _db.Ribbons
            .Where(r => r.Game == game)
            .OrderBy(r => r.RibbonName)
            .ToListAsync(ct);

    public async Task<Ribbon?> GetRibbonByIdAsync(int ribbonId, CancellationToken ct = default) =>
        await _db.Ribbons
            .FirstOrDefaultAsync(r => r.RibbonId == ribbonId, ct);
}
