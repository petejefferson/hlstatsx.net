namespace HLStatsX.NET.Core.Models;

/// <summary>Summary statistics for the game home page, mirroring the PHP game.php summary line.</summary>
public record GameStats(
    long TotalKills,
    long TotalHeadshots,
    int TotalServers,
    int Trend24hPlayers,   // snapshot value 24 h ago; -1 = no data
    long Trend24hKills     // snapshot value 24 h ago; -1 = no data
)
{
    public long NewKillsLast24h => Trend24hKills < 0 ? -1 : TotalKills - Trend24hKills;
    public double HeadshotPercent => TotalKills > 0 ? Math.Round((double)TotalHeadshots / TotalKills * 100, 2) : 0;
}
