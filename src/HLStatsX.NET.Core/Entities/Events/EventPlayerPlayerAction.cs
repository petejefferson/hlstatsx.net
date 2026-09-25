namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a two-player in-game action event where one player performs an action against another
/// (e.g. a knife stab, a headshot ribbon trigger, or a domination event).
/// <see cref="Bonus"/> is the skill-point reward credited to <see cref="PlayerId"/>.
/// </summary>
public class EventPlayerPlayerAction
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public int VictimId { get; set; }
    public int ActionId { get; set; }
    public int Bonus { get; set; }
    public DateTime? EventTime { get; set; }
    public string? Map { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? PosZ { get; set; }
    public int? PosVictimX { get; set; }
    public int? PosVictimY { get; set; }
    public int? PosVictimZ { get; set; }
}
