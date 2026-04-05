namespace HLStatsX.NET.Core.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Team { get; set; }
    public string? Image { get; set; }

    public Game? GameNavigation { get; set; }
}
