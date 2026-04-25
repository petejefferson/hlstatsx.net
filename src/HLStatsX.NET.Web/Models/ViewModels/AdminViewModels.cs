using System.ComponentModel.DataAnnotations;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

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

public record AdminUserListViewModel(IReadOnlyList<AdminUser> Users);

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

public class EditAdminUserViewModel
{
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string? ConfirmPassword { get; set; }

    [Range(1, 100)]
    public int AccLevel { get; set; } = 10;
}

public class GameFormViewModel
{
    [Required, MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public string? RealGame { get; set; }
    public bool Hidden { get; set; }
}

// Generic wrapper that carries the active game alongside any payload
public record GameScopedViewModel<T>(T Data, string Game);

public class ServerFormViewModel
{
    [Required]
    public string Game { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Address { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 27005;

    public string? PublicAddress { get; set; }
    public string? RconPassword { get; set; }
    public int SortOrder { get; set; }
}

public record ServerSettingsViewModel(
    Server Server,
    IReadOnlyList<ServerConfig> Config,
    IReadOnlyList<Server> AllServers
);

public record AdminEventsViewModel(
    IReadOnlyList<AdminEvent> Events,
    string? EventType,
    int Page,
    int PageSize,
    int Total
)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class ResetViewModel
{
    public string? Game { get; set; }
    public bool ClearAwards { get; set; }
    public bool ClearHistory { get; set; }
    public bool ClearPlayerNames { get; set; }
    public bool ClearSkill { get; set; }
    public bool ClearCounts { get; set; }
    public bool ClearMapData { get; set; }
    public bool ClearBans { get; set; }
    public bool ClearEvents { get; set; }
    public bool DeletePlayers { get; set; }
}

public class CleanupViewModel
{
    public string? Game { get; set; }
    [Range(0, int.MaxValue)]
    public int MinKills { get; set; } = 1;
}

public class EditDetailsSearchViewModel
{
    public string? Query { get; set; }
    public string? Type { get; set; }
}

public record EditPlayerViewModel(Player Player, IReadOnlyList<(string IpAddress, DateTime LastUsed)> Ips);

public class EditPlayerFormModel
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Homepage { get; set; }
    public string? Flag { get; set; }
    public int Skill { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Headshots { get; set; }
    public int Suicides { get; set; }
    public int HideRanking { get; set; }
    public int BlockAvatar { get; set; }
}
