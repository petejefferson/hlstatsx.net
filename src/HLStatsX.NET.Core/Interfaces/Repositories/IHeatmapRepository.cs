using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

/// <summary>
/// Data access contract for kill/death heatmap data overlaid on map images.
/// Points are stored per-event and binned into a grid of <c>binSize × binSize</c> squares.
/// </summary>
public interface IHeatmapRepository
{
    /// <summary>
    /// Returns the heatmap image configuration (origin offsets, scale) for a map,
    /// or <c>null</c> if no heatmap has been configured for it.
    /// </summary>
    Task<HeatmapConfig?> GetConfigAsync(string game, string map, CancellationToken ct = default);

    /// <summary>Returns aggregated kill positions on a map for the past <paramref name="days"/> days.</summary>
    Task<IReadOnlyList<HeatPoint>> GetKillPointsAsync(string game, string map, int days, int binSize, int? playerId, CancellationToken ct = default);

    /// <summary>Returns aggregated death positions on a map for the past <paramref name="days"/> days.</summary>
    Task<IReadOnlyList<HeatPoint>> GetDeathPointsAsync(string game, string map, int days, int binSize, int? playerId, CancellationToken ct = default);
}
