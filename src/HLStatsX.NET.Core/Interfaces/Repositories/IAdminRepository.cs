using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IAdminRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(AdminUser user, CancellationToken ct = default);
    Task UpdateAsync(AdminUser user, CancellationToken ct = default);
    Task DeleteAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<Option>> GetOptionsAsync(CancellationToken ct = default);
    Task SetOptionAsync(string keyName, string value, CancellationToken ct = default);
}
