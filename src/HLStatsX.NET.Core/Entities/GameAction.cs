namespace HLStatsX.NET.Core.Entities;

public class GameAction
{
    public int ActionId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Team { get; set; }
    public int RewardPlayer { get; set; }
    public int RewardTeam { get; set; }
    public int Count { get; set; }
    public bool ForPlayerActions { get; set; }
    public bool ForPlayerPlayerActions { get; set; }
    public bool ForTeamActions { get; set; }
    public bool ForWorldActions { get; set; }

    public Game? GameNavigation { get; set; }
}
