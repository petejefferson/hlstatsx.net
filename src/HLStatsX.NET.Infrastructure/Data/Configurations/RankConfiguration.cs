using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class RankConfiguration : IEntityTypeConfiguration<Rank>
{
    public void Configure(EntityTypeBuilder<Rank> builder)
    {
        builder.ToTable("hlstats_Ranks");
        builder.HasKey(r => r.RankId);
        builder.Property(r => r.RankId).HasColumnName("rankId");
        builder.Property(r => r.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(r => r.RankName).HasColumnName("rankName").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Image).HasColumnName("image").HasMaxLength(30);
        builder.Property(r => r.MinKills).HasColumnName("minKills");
        builder.Property(r => r.MaxKills).HasColumnName("maxKills");
    }
}
