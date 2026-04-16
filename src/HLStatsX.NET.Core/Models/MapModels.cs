namespace HLStatsX.NET.Core.Models;

public record MapPlayerRow(
    int PlayerId,
    string PlayerName,
    string? Flag,
    long Kills,
    long Headshots,
    double HeadshotsPerKill
);
