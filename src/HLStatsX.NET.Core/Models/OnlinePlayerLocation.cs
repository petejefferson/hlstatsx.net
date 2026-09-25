namespace HLStatsX.NET.Core.Models;

/// <summary>GeoIP location of an online player, used to place markers on the game dashboard map.</summary>
public record OnlinePlayerLocation(
    int PlayerId,
    string Name,
    string? Country,
    string? City,
    float Lat,
    float Lng,
    long Kills,
    long Deaths
);
