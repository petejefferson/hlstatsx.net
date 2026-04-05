namespace HLStatsX.NET.Core.Entities.Events;

public class EventChat
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MessageMode { get; set; }
    public string Map { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }

    public Player? Player { get; set; }
    public Server? Server { get; set; }
}
