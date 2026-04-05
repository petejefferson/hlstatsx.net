using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly HLStatsDbContext _db;

    public AdminRepository(HLStatsDbContext db) => _db = db;

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await _db.AdminUsers.FindAsync(new object[] { username }, ct);

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default) =>
        await _db.AdminUsers.OrderBy(u => u.Username).ToListAsync(ct);

    public async Task AddAsync(AdminUser user, CancellationToken ct = default)
    {
        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken ct = default)
    {
        _db.AdminUsers.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string username, CancellationToken ct = default)
    {
        var user = await _db.AdminUsers.FindAsync(new object[] { username }, ct);
        if (user is not null)
        {
            _db.AdminUsers.Remove(user);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default) =>
        await _db.Options.OrderBy(o => o.KeyName).ToListAsync(ct);

    public async Task SetOptionAsync(string keyName, string value, CancellationToken ct = default)
    {
        var option = await _db.Options.FindAsync(new object[] { keyName }, ct);
        if (option is not null)
        {
            option.Value = value;
            _db.Options.Update(option);
            await _db.SaveChangesAsync(ct);
        }
    }
}
