using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record PlayerLeaderboardViewModel(
    PagedResult<Player> Players,
    string Game,
    string SortBy,
    bool Descending
);

public record PlayerProfileViewModel(
    Player Player,
    int Rank,
    Rank? CurrentRank,
    IReadOnlyList<PlayerName> Aliases,
    IReadOnlyList<PlayerAward> Awards,
    IReadOnlyList<PlayerRibbon> Ribbons
);

public record PlayerHistoryViewModel(
    Player Player,
    IReadOnlyList<PlayerHistory> History,
    int Days
);

public record BanListViewModel(
    IReadOnlyList<Player> BannedPlayers,
    string Game
);
