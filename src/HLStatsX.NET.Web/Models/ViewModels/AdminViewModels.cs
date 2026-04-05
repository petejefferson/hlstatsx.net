using System.ComponentModel.DataAnnotations;
using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public record AdminDashboardViewModel(
    int TotalPlayers,
    int TotalClans,
    int ActiveServers,
    IReadOnlyList<Server> Servers,
    Dictionary<string, string> Options
);

public record AdminUserListViewModel(
    IReadOnlyList<AdminUser> Users
);

public class CreateAdminUserViewModel
{
    [Required, MaxLength(16)]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(1, 100)]
    public int AccLevel { get; set; } = 10;
}
