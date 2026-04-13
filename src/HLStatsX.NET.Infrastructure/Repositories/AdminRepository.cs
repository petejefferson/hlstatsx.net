using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public AdminRepository(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.AdminUsers.FindAsync(new object[] { username }, ct);
    }

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.AdminUsers.OrderBy(u => u.Username).ToListAsync(ct);
    }

    public async Task AddAsync(AdminUser user, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.AdminUsers.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string username, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var user = await db.AdminUsers.FindAsync(new object[] { username }, ct);
        if (user is not null)
        {
            db.AdminUsers.Remove(user);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Options.OrderBy(o => o.KeyName).ToListAsync(ct);
    }

    public async Task SetOptionAsync(string keyName, string value, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var option = await db.Options.FindAsync(new object[] { keyName }, ct);
        if (option is not null)
        {
            option.Value = value;
            db.Options.Update(option);
            await db.SaveChangesAsync(ct);
        }
    }
}
