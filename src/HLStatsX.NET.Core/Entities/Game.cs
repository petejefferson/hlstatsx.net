namespace HLStatsX.NET.Core.Entities;

public class Game
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RealGame { get; set; }
    public string? Hidden { get; set; }

    public bool IsHidden => Hidden == "1";

    public ICollection<Server> Servers { get; set; } = new List<Server>();
    public ICollection<Weapon> Weapons { get; set; } = new List<Weapon>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();
}
