namespace HLStatsX.NET.Core.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Picked { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public string Hidden { get; set; } = "0";

    public Game? GameNavigation { get; set; }
}
