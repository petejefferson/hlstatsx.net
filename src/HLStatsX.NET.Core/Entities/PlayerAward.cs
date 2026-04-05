namespace HLStatsX.NET.Core.Entities;

public class PlayerAward
{
    public DateTime AwardTime { get; set; }
    public int AwardId { get; set; }
    public int PlayerId { get; set; }
    public int Count { get; set; }
    public string Game { get; set; } = string.Empty;

    public Player? Player { get; set; }
    public Award? Award { get; set; }
}
