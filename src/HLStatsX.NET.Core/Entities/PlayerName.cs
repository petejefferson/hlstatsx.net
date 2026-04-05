namespace HLStatsX.NET.Core.Entities;

public class PlayerName
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LastUse { get; set; }
    public int Numuses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int ConnectionTime { get; set; }

    public Player? Player { get; set; }
}
