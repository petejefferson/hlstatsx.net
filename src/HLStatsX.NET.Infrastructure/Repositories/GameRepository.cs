using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public GameRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<Game?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Games.FirstOrDefaultAsync(g => g.Code == code, ct);
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Games.Where(g => g.Hidden == "0").OrderBy(g => g.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Teams.Where(t => t.Game == game).OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Roles.Where(r => r.Game == game).OrderBy(r => r.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GameAction>> GetActionsAsync(string game, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GameActions.Where(a => a.Game == game).OrderBy(a => a.Description).ToListAsync(ct);
    }
}
