using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Services;

/// <summary>
/// Service contract for server data, live player counts, load history, and trend series.
/// </summary>
public interface IServerService
{
    /// <summary>
    /// Returns game-wide aggregate statistics (total kills, players, playtime)
    /// displayed in the leaderboard header.
    /// </summary>
    Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a server by primary key, or <c>null</c> if not found.</summary>
    Task<Server?> GetServerAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns all servers, optionally filtered by game code.</summary>
    Task<IReadOnlyList<Server>> GetServersAsync(string? game = null, CancellationToken ct = default);

    /// <summary>Returns the live player list for a single server.</summary>
    Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns live player data across all servers for the given game.</summary>
    Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns hourly player-count samples for all servers over the past <paramref name="hours"/> hours.</summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int hours, CancellationToken ct = default);

    /// <summary>Returns hourly player-count samples for a single server over the past <paramref name="hours"/> hours.</summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadByServerIdAsync(int serverId, int hours, CancellationToken ct = default);

    /// <summary>
    /// Returns down-sampled server load data for the given time range.
    /// <paramref name="range"/>: 1 = 24h, 2 = 1 week, 3 = 1 month, 4 = 1 year.
    /// </summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadRangedAsync(int serverId, int range, CancellationToken ct = default);

    /// <summary>Returns a time-series used to render the player-count trend sparkline.</summary>
    Task<IReadOnlyList<Trend>> GetTrendSeriesAsync(string game, int hours, CancellationToken ct = default);

    /// <summary>Returns all teams defined for the given game.</summary>
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);

    /// <summary>Inserts a new server and returns the entity with its generated primary key set.</summary>
    Task<Server> CreateServerAsync(Server server, CancellationToken ct = default);

    /// <summary>Persists changes to an existing server record.</summary>
    Task UpdateServerAsync(Server server, CancellationToken ct = default);

    /// <summary>Deletes a server and its config entries by primary key.</summary>
    Task DeleteServerAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns all configured voice-comm servers (TeamSpeak / Ventrilo) from the DB.</summary>
    Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns GeoIP locations for all online players of the given game
    /// who have non-zero lat/lng on their player record.
    /// </summary>
    Task<IReadOnlyList<OnlinePlayerLocation>> GetOnlinePlayerLocationsAsync(string game, CancellationToken ct = default);
}
