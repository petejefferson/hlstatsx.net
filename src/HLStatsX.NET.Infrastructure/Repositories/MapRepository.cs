using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class MapRepository : IMapRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public MapRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<MapCount?> GetByNameAsync(string mapName, string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.MapCounts.FirstOrDefaultAsync(m => m.Map == mapName && m.Game == game, ct);
    }

    public async Task<PagedResult<MapCount>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();

        var query = db.MapCounts.Where(m => m.Game == game);

        query = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("headshots", true)  => query.OrderByDescending(m => m.Headshots),
            ("headshots", false) => query.OrderBy(m => m.Headshots),
            (_, true)            => query.OrderByDescending(m => m.Kills),
            (_, false)           => query.OrderBy(m => m.Kills)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<MapCount>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<MapCount>> GetTopMapsAsync(string game, int count = 10, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.MapCounts
            .Where(m => m.Game == game)
            .OrderByDescending(m => m.Kills)
            .Take(count)
            .ToListAsync(ct);
    }
}
