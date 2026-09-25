using HLStatsX.NET.Core.Entities.Events;

namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Represents a weapon within a game. Maps to <c>hlstats_Weapons</c>.
/// The <see cref="Modifier"/> affects how much skill a kill with this weapon is worth.
/// </summary>
public class Weapon
{
    public int WeaponId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Multiplier applied to skill-point gains for kills with this weapon. Default is <c>1.0</c>.</summary>
    public float Modifier { get; set; } = 1.0f;
    public int Kills { get; set; }
    public int Headshots { get; set; }

    public Game? GameNavigation { get; set; }
    public ICollection<EventFrag> FragEvents { get; set; } = new List<EventFrag>();

    /// <summary>Headshots as a percentage of kills with this weapon, rounded to 1 decimal place.</summary>
    public double HeadshotPercent => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
}
