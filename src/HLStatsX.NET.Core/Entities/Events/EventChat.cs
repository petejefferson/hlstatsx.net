namespace HLStatsX.NET.Core.Entities.Events;

/// <summary>
/// A chat message sent by a player in-game. Maps to <c>hlstats_Events_Chats</c>.
/// <see cref="MessageMode"/> indicates the chat channel: <c>0</c> = all chat, <c>1</c> = team chat.
/// </summary>
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
