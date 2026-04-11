using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record ClanLeaderboardViewModel(
    PagedResult<Clan> Clans,
    string Game,
    string SortBy,
    bool Descending
);

public record ClanProfileViewModel(
    Clan Clan,
    IReadOnlyList<Player> Members,
    string Game
);
