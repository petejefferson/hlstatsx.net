namespace HLStatsX.NET.Core.Entities;

public class Award
{
    public int AwardId { get; set; }
    public string AwardType { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Verb { get; set; } = string.Empty;
    public int? DailyWinnerId { get; set; }
    public int? DailyWinnerCount { get; set; }
    public int? GlobalWinnerId { get; set; }
    public int? GlobalWinnerCount { get; set; }

    public Player? DailyWinner { get; set; }
    public Player? GlobalWinner { get; set; }

    public ICollection<PlayerAward> PlayerAwards { get; set; } = new List<PlayerAward>();
}
