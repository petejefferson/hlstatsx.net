using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Core.Interfaces.Services;

public interface IAdminService
{
    Task<AdminUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default);
    Task CreateUserAsync(AdminUser user, string password, CancellationToken ct = default);
    Task UpdateUserAsync(AdminUser user, CancellationToken ct = default);
    Task DeleteUserAsync(string username, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetOptionsAsync(CancellationToken ct = default);
    Task SetOptionAsync(string keyName, string value, CancellationToken ct = default);
}
