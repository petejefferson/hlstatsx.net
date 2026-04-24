namespace HLStatsX.NET.Core.Models;

public record WeaponKillerRow(
    int PlayerId,
    string PlayerName,
    string? Flag,
    int Frags,
    int Headshots
)
{
    public double Hpk => Frags == 0 ? 0.0 : Math.Round((double)Headshots / Frags, 2);
}
