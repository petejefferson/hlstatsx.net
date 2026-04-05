namespace HLStatsX.NET.Core.Entities;

public class Clan
{
    public int ClanId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public string MapRegion { get; set; } = string.Empty;
    public bool IsHidden { get; set; }

    public ICollection<Player> Players { get; set; } = new List<Player>();
}
