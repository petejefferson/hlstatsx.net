namespace HLStatsX.NET.Core.Models;

public record GameListRow(
    string Code,
    string Name,
    int ActPlayers,
    int MaxPlayers,
    int? TopPlayerId,
    string? TopPlayerName,
    int? TopClanId,
    string? TopClanName
);

public record GamesListData(
    IReadOnlyList<GameListRow> Games,
    int TotalPlayers,
    int TotalClans,
    int TotalServers,
    long TotalKills,
    DateTime? LastKillTime,
    int DeleteDays
);
