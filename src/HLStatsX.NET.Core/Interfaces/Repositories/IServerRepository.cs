using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for servers, live player data, and load history.
/// </summary>
public interface IServerRepository
{
    /// <summary>
    /// Returns aggregate game-wide statistics (total kills, players, playtime, etc.)
    /// for the summary header shown on the home page and leaderboard.
    /// </summary>
    Task<GameStats> GetGameStatsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns a server by primary key, or <c>null</c> if not found.</summary>
    Task<Server?> GetByIdAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns all servers, optionally filtered by game code.</summary>
    Task<IReadOnlyList<Server>> GetAllAsync(string? game = null, CancellationToken ct = default);

    /// <summary>Returns only servers that have sent an event within the active window.</summary>
    Task<IReadOnlyList<Server>> GetActiveAsync(string? game = null, CancellationToken ct = default);

    /// <summary>Returns the live player list for a single server from <c>hlstats_Livestats</c>.</summary>
    Task<IReadOnlyList<Livestat>> GetLivestatsAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns live player data across all servers for the given game.</summary>
    Task<IReadOnlyList<Livestat>> GetAllLivestatsAsync(string game, CancellationToken ct = default);

    /// <summary>Returns hourly player-count samples for the past <paramref name="hours"/> hours across all servers.</summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadAsync(string game, int hours, CancellationToken ct = default);

    /// <summary>Returns hourly player-count samples for a single server for the past <paramref name="hours"/> hours.</summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadByServerIdAsync(int serverId, int hours, CancellationToken ct = default);

    /// <summary>Returns all server load rows for a single server (no time cutoff), ordered by timestamp ASC.</summary>
    Task<IReadOnlyList<ServerLoad>> GetServerLoadAllByServerIdAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns a time-series of aggregate player counts used to render the trend sparkline.</summary>
    Task<IReadOnlyList<Trend>> GetTrendSeriesAsync(string game, int hours, CancellationToken ct = default);

    /// <summary>Returns all teams defined for the given game (used to populate team columns).</summary>
    Task<IReadOnlyList<Team>> GetTeamsAsync(string game, CancellationToken ct = default);

    /// <summary>Persists changes to an existing server record.</summary>
    Task UpdateAsync(Server server, CancellationToken ct = default);

    /// <summary>Inserts a new server record.</summary>
    Task AddAsync(Server server, CancellationToken ct = default);

    /// <summary>Deletes a server record by primary key.</summary>
    Task DeleteAsync(int serverId, CancellationToken ct = default);

    /// <summary>Returns all configured voice-comm servers from <c>hlstats_Servers_VoiceComm</c>.</summary>
    Task<IReadOnlyList<VoiceCommServer>> GetVoiceCommServersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns GeoIP locations for all players currently listed in <c>hlstats_Livestats</c>
    /// for the given game who have non-zero lat/lng on their player record.
    /// </summary>
    Task<IReadOnlyList<OnlinePlayerLocation>> GetOnlinePlayerLocationsAsync(string game, CancellationToken ct = default);
}
