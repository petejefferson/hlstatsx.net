namespace HLStatsX.NET.Core.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Game? GameNavigation { get; set; }
}
