using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record MapListViewModel(
    PagedResult<MapCount> Maps,
    string Game,
    string SortBy,
    bool Descending
);
