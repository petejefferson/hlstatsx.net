namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Represents a game supported by HLStatsX, e.g. <c>dods</c> or <c>cstrike</c>.
/// Maps to <c>hlstats_Games</c>. Weapons, teams, roles, and actions are all game-scoped.
/// </summary>
public class Game
{
    /// <summary>Short identifier used throughout the database, e.g. <c>"dods"</c>.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Base game code when this entry is a mod, e.g. a GunGame mod might set
    /// <c>RealGame = "cstrike"</c> to inherit weapon and map data.
    /// </summary>
    public string? RealGame { get; set; }
    /// <summary>Raw database value; use <see cref="IsHidden"/> for boolean access.</summary>
    public string? Hidden { get; set; }

    /// <summary><c>true</c> when the game is hidden from public game-selection pages.</summary>
    public bool IsHidden => Hidden == "1";

    public ICollection<Server> Servers { get; set; } = new List<Server>();
    public ICollection<Weapon> Weapons { get; set; } = new List<Weapon>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();
}
