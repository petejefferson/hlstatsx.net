using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class HeatmapConfigConfiguration : IEntityTypeConfiguration<HeatmapConfig>
{
    public void Configure(EntityTypeBuilder<HeatmapConfig> builder)
    {
        builder.ToTable("hlstats_Heatmap_Config");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(h => h.Game).HasColumnName("game").HasMaxLength(32);
        builder.Property(h => h.XOffset).HasColumnName("xoffset");
        builder.Property(h => h.YOffset).HasColumnName("yoffset");
        builder.Property(h => h.FlipX).HasColumnName("flipx");
        builder.Property(h => h.FlipY).HasColumnName("flipy");
        builder.Property(h => h.Rotate).HasColumnName("rotate");
        builder.Property(h => h.Days).HasColumnName("days");
        builder.Property(h => h.Scale).HasColumnName("scale");
    }
}
