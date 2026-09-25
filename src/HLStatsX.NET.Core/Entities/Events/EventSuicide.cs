namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>Records a suicide event — a player killing themselves (fall damage, grenade, etc.).</summary>
public class EventSuicide
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string WeaponCode { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? PosZ { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
