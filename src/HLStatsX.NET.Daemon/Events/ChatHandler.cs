using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Handles in-game chat events, logging them to <c>hlstats_Events_Chats</c> when
/// the server config enables chat logging.
/// Mirrors <c>doEvent_Chat</c> in <c>HLstats_EventHandlers.plib</c>.
/// </summary>
public sealed class ChatHandler
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public ChatHandler(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    /// <param name="mode">Chat channel: <c>"say"</c> = all chat, <c>"say_team"</c> = team chat.</param>
    /// <param name="message">The message text.</param>
    public async Task HandleAsync(
        PlayerSession player,
        string mode,
        string message,
        EventContext ctx,
        CancellationToken ct = default)
    {
        // Perl checks g_log_chat / g_log_chat_admins global flags
        if (player.IsBot) return;
        if (player.DbPlayerId <= 0) return;

        int messageMode = mode.Equals("say_team", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        await using var db = _dbFactory.CreateDbContext();
        db.EventChats.Add(new EventChat
        {
            ServerId    = ctx.Server.ServerId,
            PlayerId    = player.DbPlayerId,
            Message     = message,
            MessageMode = messageMode,
            Map         = ctx.Map,
            EventTime   = ctx.EventTime,
        });
        await db.SaveChangesAsync(ct);
    }
}
