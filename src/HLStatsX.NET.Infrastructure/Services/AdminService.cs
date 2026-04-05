using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace HLStatsX.NET.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _admin;

    public AdminService(IAdminRepository admin) => _admin = admin;

    public async Task<AdminUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _admin.GetByUsernameAsync(username, ct);
        if (user is null) return null;

        // Legacy HLStatsX stores passwords as MD5
        var hash = ComputeMd5(password);
        return string.Equals(user.Password, hash, StringComparison.OrdinalIgnoreCase) ? user : null;
    }

    public Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken ct = default) =>
        _admin.GetAllAsync(ct);

    public async Task CreateUserAsync(AdminUser user, string password, CancellationToken ct = default)
    {
        user.Password = ComputeMd5(password);
        await _admin.AddAsync(user, ct);
    }

    public Task UpdateUserAsync(AdminUser user, CancellationToken ct = default) =>
        _admin.UpdateAsync(user, ct);

    public Task DeleteUserAsync(string username, CancellationToken ct = default) =>
        _admin.DeleteAsync(username, ct);

    public async Task<Dictionary<string, string>> GetOptionsAsync(CancellationToken ct = default)
    {
        var options = await _admin.GetOptionsAsync(ct);
        return options.ToDictionary(o => o.KeyName, o => o.Value);
    }

    public Task SetOptionAsync(string name, string value, CancellationToken ct = default) =>
        _admin.SetOptionAsync(name, value, ct);

    private static string ComputeMd5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
