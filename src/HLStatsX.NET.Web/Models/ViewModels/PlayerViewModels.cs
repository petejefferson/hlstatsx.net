using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record PlayerLeaderboardViewModel(
    PagedResult<PlayerLeaderboardRow> Players,
    string Game,
    string SortBy,
    bool Descending,
    IReadOnlyList<Rank> Ranks,
    string RankType,
    IReadOnlyList<DateTime> AvailableDates
)
{
    public bool IsHistorical => RankType != "total";
    public Rank? GetArmyRank(int kills) =>
        Ranks.Where(r => r.MinKills <= kills).MaxBy(r => r.MinKills);
}

public record PlayerProfileViewModel(
    Player Player,
    int Rank,
    Rank? CurrentRank,
    Rank? NextRank,
    IReadOnlyList<Rank> PastRanks,
    IReadOnlyList<PlayerName> Aliases,
    IReadOnlyList<PlayerAward> Awards,
    IReadOnlyList<RibbonDisplay> AllRibbons,
    RealStats RealStats,
    PingStats? Ping,
    DateTime? LastConnect,
    FavoriteServer? FavoriteServer,
    string? FavoriteMap,
    FavoriteWeapon? FavoriteWeapon,
    IReadOnlyList<KillStatRow> KillStats,
    IReadOnlyList<MapStatRow> MapPerformance,
    IReadOnlyList<ServerStatRow> ServerPerformance,
    IReadOnlyList<WeaponStatRow> WeaponStats,
    IReadOnlyList<TeamStatRow> TeamSelection,
    IReadOnlyList<RoleStatRow> RoleSelection,
    IReadOnlyList<ActionStatRow> PlayerActions,
    IReadOnlyList<ActionStatRow> PlayerActionVictims
)
{
    public double RankPercent => CurrentRank is null || NextRank is null ? 0
        : NextRank.MinKills == CurrentRank.MinKills ? 100
        : Math.Round((Player.Kills - CurrentRank.MinKills) * 100.0 / (NextRank.MinKills - CurrentRank.MinKills), 0);

    public int RankKillsNeeded => NextRank is null ? 0 : Math.Max(0, NextRank.MinKills - Player.Kills);
}

public record PlayerHistoryViewModel(
    Player Player,
    IReadOnlyList<PlayerHistory> History,
    int Days
);

public record BanListViewModel(
    IReadOnlyList<Player> BannedPlayers,
    string Game
);
