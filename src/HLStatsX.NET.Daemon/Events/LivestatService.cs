using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Manages per-player rows in <c>hlstats_Livestats</c> — the live player tracking
/// table that feeds <c>livestats.php</c>.
/// Mirrors <c>insertPlayerLivestats</c>, <c>deleteLivestats</c>, and the live-stats
/// section of <c>flushDB</c> in <c>HLstats_Player.pm</c>.
/// </summary>
public sealed class LivestatService
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;

    public LivestatService(IDbContextFactory<HLStatsDbContext> dbFactory)
        => _dbFactory = dbFactory;

    /// <summary>
    /// Inserts or replaces the player's live-stats row on connect.
    /// Mirrors Perl <c>insertPlayerLivestats</c> (REPLACE INTO).
    /// </summary>
    public async Task UpsertAsync(PlayerSession session, CancellationToken ct = default)
    {
        if (session.DbPlayerId <= 0 || session.ServerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            REPLACE INTO hlstats_Livestats
              (player_id, server_id, cli_address, steam_id, name, team,
               ping, connected, skill, cli_flag)
            VALUES
              ({0}, {1}, {2}, {3}, {4}, {5}, 0, {6}, {7}, '')
            """,
            new object[] { session.DbPlayerId, session.ServerId, session.IpAddress,
                           session.PlainUniqueId, session.Name, session.Team,
                           (int)session.ConnectTime, session.Skill },
            ct);
    }

    /// <summary>
    /// Removes the player's live-stats row on disconnect.
    /// Mirrors Perl <c>deleteLivestats</c>.
    /// </summary>
    public async Task DeleteAsync(int playerId, CancellationToken ct = default)
    {
        if (playerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM hlstats_Livestats WHERE player_id = {0}",
            new object[] { playerId },
            ct);
    }

    /// <summary>
    /// Updates the player's live-stats row with current session accumulators.
    /// Called from <c>FlushSessionAsync</c> to keep in-game stats current.
    /// Mirrors the live-stats UPDATE in Perl <c>flushDB</c>.
    /// </summary>
    public async Task UpdateAsync(PlayerSession session, CancellationToken ct = default)
    {
        if (session.DbPlayerId <= 0) return;

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE hlstats_Livestats SET
              cli_address  = {0},
              steam_id     = {1},
              name         = {2},
              team         = {3},
              kills        = {4},
              deaths       = {5},
              suicides     = {6},
              headshots    = {7},
              shots        = {8},
              hits         = {9},
              skill_change = {10},
              skill        = {11}
            WHERE player_id = {12}
            """,
            new object[] { session.IpAddress, session.PlainUniqueId, session.Name,
                           session.Team, session.SessionKills, session.SessionDeaths,
                           session.SessionSuicides, session.SessionHeadshots,
                           session.SessionShots, session.SessionHits,
                           session.SessionSkillChange, session.Skill, session.DbPlayerId },
            ct);
    }
}
