namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// Logged when a player connects to a server. Maps to <c>hlstats_Events_Connects</c>.
/// The matching disconnect time is stored in <see cref="EventTimeDisconnect"/> and is
/// null until the player leaves.
/// </summary>
public class EventConnect
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string HostGroup { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime? EventTime { get; set; }
    public DateTime? EventTimeDisconnect { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
