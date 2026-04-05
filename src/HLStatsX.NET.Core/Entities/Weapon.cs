using HLStatsX.NET.Core.Entities.Events;

namespace HLStatsX.NET.Core.Entities;

public class Weapon
{
    public int WeaponId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float Modifier { get; set; } = 1.0f;
    public int Kills { get; set; }
    public int Headshots { get; set; }

    public Game? GameNavigation { get; set; }
    public ICollection<EventFrag> FragEvents { get; set; } = new List<EventFrag>();

    public double HeadshotPercent => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
}
