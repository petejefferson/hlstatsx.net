using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class TrendConfiguration : IEntityTypeConfiguration<Trend>
{
    public void Configure(EntityTypeBuilder<Trend> builder)
    {
        builder.ToTable("hlstats_Trend");
        builder.HasKey(t => new { t.Timestamp, t.Game });
        builder.Property(t => t.Timestamp).HasColumnName("timestamp");
        builder.Property(t => t.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(t => t.Players).HasColumnName("players");
        builder.Property(t => t.Kills).HasColumnName("kills");
        builder.Property(t => t.Headshots).HasColumnName("headshots");
        builder.Property(t => t.Servers).HasColumnName("servers");
    }
}
