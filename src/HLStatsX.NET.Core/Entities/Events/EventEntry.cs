namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Records a player joining (entering) a server. This is the initial entry event,
/// distinct from <see cref="EventConnect"/> which includes the player's IP and Steam ID.
/// </summary>
public class EventEntry
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string? Map { get; set; }
    public DateTime? EventTime { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
