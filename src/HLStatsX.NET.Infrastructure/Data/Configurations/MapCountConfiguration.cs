using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class MapCountConfiguration : IEntityTypeConfiguration<MapCount>
{
    public void Configure(EntityTypeBuilder<MapCount> builder)
    {
        builder.ToTable("hlstats_Maps_Counts");
        builder.HasKey(m => m.RowId);
        builder.Property(m => m.RowId).HasColumnName("rowId");
        builder.Property(m => m.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(m => m.Map).HasColumnName("map").HasMaxLength(64).IsRequired();
        builder.Property(m => m.Kills).HasColumnName("kills");
        builder.Property(m => m.Headshots).HasColumnName("headshots");

        builder.HasIndex(m => new { m.Game, m.Map });
    }
}
