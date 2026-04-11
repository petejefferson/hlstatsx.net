using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Core.Interfaces.Repositories;

public interface IMapRepository
{
    Task<MapCount?> GetByNameAsync(string mapName, string game, CancellationToken ct = default);
    Task<PagedResult<MapCount>> GetAllAsync(string game, int page, int pageSize, string sortBy = "kills", bool desc = true, CancellationToken ct = default);
    Task<IReadOnlyList<MapCount>> GetTopMapsAsync(string game, int count = 10, CancellationToken ct = default);
}
