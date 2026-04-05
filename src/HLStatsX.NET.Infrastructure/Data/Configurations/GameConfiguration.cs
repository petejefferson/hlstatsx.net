using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("hlstats_Games");
        builder.HasKey(g => g.Code);
        builder.Property(g => g.Code).HasColumnName("code").HasMaxLength(32);
        builder.Property(g => g.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        builder.Property(g => g.RealGame).HasColumnName("realgame").HasMaxLength(32);
        builder.Property(g => g.Hidden).HasColumnName("hidden").HasMaxLength(1);

        builder.Ignore(g => g.IsHidden);
    }
}
