namespace HLStatsX.NET.Core.Models;

public record RoleKillerRow(
    int PlayerId,
    string PlayerName,
    string? Flag,
    int Frags
);
