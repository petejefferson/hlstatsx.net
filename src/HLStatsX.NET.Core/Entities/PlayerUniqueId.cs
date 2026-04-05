namespace HLStatsX.NET.Core.Entities;

public class PlayerUniqueId
{
    public int PlayerId { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public int Merge { get; set; }

    public Player? Player { get; set; }
}
