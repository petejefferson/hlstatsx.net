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

public record PlayerProfileViewModel
{
    public required Player Player { get; init; }
    public required int Rank { get; init; }
    public required Rank? CurrentRank { get; init; }
    public required Rank? NextRank { get; init; }
    public required IReadOnlyList<Rank> PastRanks { get; init; }
    public required IReadOnlyList<PlayerName> Aliases { get; init; }
    public required IReadOnlyList<PlayerAward> Awards { get; init; }
    public required IReadOnlyList<RibbonDisplay> AllRibbons { get; init; }
    public required RealStats RealStats { get; init; }
    public required PingStats? Ping { get; init; }
    public required DateTime? LastConnect { get; init; }
    public required FavoriteServer? FavoriteServer { get; init; }
    public required string? FavoriteMap { get; init; }
    public required FavoriteWeapon? FavoriteWeapon { get; init; }
    public required IReadOnlyList<KillStatRow> KillStats { get; init; }
    public required IReadOnlyList<MapStatRow> MapPerformance { get; init; }
    public required IReadOnlyList<ServerStatRow> ServerPerformance { get; init; }
    public required IReadOnlyList<WeaponStatRow> WeaponStats { get; init; }
    public required IReadOnlyList<TeamStatRow> TeamSelection { get; init; }
    public required IReadOnlyList<RoleStatRow> RoleSelection { get; init; }
    public required IReadOnlyList<ActionStatRow> PlayerActions { get; init; }
    public required IReadOnlyList<ActionStatRow> PlayerActionVictims { get; init; }

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
