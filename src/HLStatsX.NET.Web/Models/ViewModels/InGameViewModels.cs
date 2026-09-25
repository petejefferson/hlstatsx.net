using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Web.Models.ViewModels;

public record InGameMotdViewModel(
    string Game,
    IReadOnlyList<Player> TopPlayers,
    IReadOnlyList<ClanLeaderboardRow> TopClans,
    IReadOnlyList<Server> Servers);

public record InGamePlayerListViewModel(
    string Game,
    PagedResult<Player> Players,
    string SortBy,
    bool Desc);

public record InGameClanListViewModel(
    string Game,
    PagedResult<ClanLeaderboardRow> Clans,
    string SortBy,
    bool Desc,
    int MinMembers);

public record InGameClanInfoViewModel(
    string Game,
    Clan Clan,
    ClanSummaryStats Summary,
    PagedResult<ClanMemberRow> Members,
    string MemberSortBy,
    bool MemberDesc);

public record InGamePlayerStatsViewModel(
    string Game,
    Player Player,
    int Rank,
    RealStats RealStats,
    double StatsmeAccuracy,
    long StatsmeShots,
    long StatsmeHits);

public record InGameKillsViewModel(
    string Game,
    Player Player,
    IReadOnlyList<KillStatRow> Kills,
    long TotalRealHeadshots,
    int KillLimit);

public record InGameWeaponsViewModel(
    string Game,
    Player Player,
    IReadOnlyList<WeaponStatRow> Weapons,
    long TotalKills,
    long TotalHeadshots);

public record InGameAccuracyViewModel(
    string Game,
    Player Player,
    IReadOnlyList<WeaponStatsmeRow> Weapons);

public record InGameTargetsViewModel(
    string Game,
    Player Player,
    IReadOnlyList<WeaponTargetRow> Targets);

public record InGameMapsViewModel(
    string Game,
    Player Player,
    IReadOnlyList<MapStatRow> Maps,
    long TotalKills,
    long TotalHeadshots);

public record InGameServersViewModel(
    string Game,
    IReadOnlyList<Server> Servers);

public record InGameStatusViewModel(
    string Game,
    Server Server,
    IReadOnlyList<Livestat> LivePlayers,
    int TotalPlayers,
    long TotalKills,
    long TotalHeadshots,
    int TotalServers);

public record InGameLoadViewModel(
    string Game,
    int TotalPlayers,
    long TotalKills,
    long TotalHeadshots,
    int TotalServers,
    int ServerId);

public record InGameBansViewModel(
    string Game,
    PagedResult<BanListRow> Bans,
    string SortBy,
    bool Desc);

public record InGameHelpViewModel(
    string Game,
    IReadOnlyList<Server> Servers);

public record InGameWeaponInfoViewModel(
    string Game,
    string WeaponCode,
    string WeaponName,
    PagedResult<WeaponKillerRow> Killers,
    string SortBy,
    bool Desc);

public record InGameMapInfoViewModel(
    string Game,
    string MapName,
    PagedResult<MapPlayerRow> Players,
    string SortBy,
    bool Desc);

public record InGameActionsViewModel(
    string Game,
    IReadOnlyList<ActionListRow> Actions,
    string SortBy,
    bool Desc);

public record InGameActionInfoViewModel(
    string Game,
    string ActionCode,
    string ActionDescription,
    PagedResult<ActionAchieverRow> Achievers,
    string SortBy,
    bool Desc,
    int Page);
