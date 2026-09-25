namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a single-player in-game action event (e.g. a bomb plant, flag capture, or objective assist).
/// For actions involving two players, see <see cref="EventPlayerPlayerAction"/>.
/// <see cref="Bonus"/> is the skill-point reward credited to the player for performing this action.
/// </summary>
public class EventPlayerAction
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public int ActionId { get; set; }
    public int Bonus { get; set; }
    public DateTime? EventTime { get; set; }
    public string? Map { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? PosZ { get; set; }
}
