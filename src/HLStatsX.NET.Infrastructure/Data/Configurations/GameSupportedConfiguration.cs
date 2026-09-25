using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class GameSupportedConfiguration : IEntityTypeConfiguration<GameSupported>
{
    public void Configure(EntityTypeBuilder<GameSupported> builder)
    {
        builder.ToTable("hlstats_Games_Supported");
        builder.HasKey(g => g.Code);
        builder.Property(g => g.Code).HasColumnName("code").HasMaxLength(32);
        builder.Property(g => g.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
    }
}
