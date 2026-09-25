namespace HLStatsX.NET.Core.Entities;

public class ServerLoad
{
    public int ServerId { get; set; }
    public int Timestamp { get; set; }
    public int ActPlayers { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string? Map { get; set; }

    public Server? Server { get; set; }

    public DateTime DateTime => DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
}
