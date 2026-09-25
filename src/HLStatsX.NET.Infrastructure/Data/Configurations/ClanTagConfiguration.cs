using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class ClanTagConfiguration : IEntityTypeConfiguration<ClanTag>
{
    public void Configure(EntityTypeBuilder<ClanTag> builder)
    {
        builder.ToTable("hlstats_ClanTags");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Pattern).HasColumnName("pattern").HasMaxLength(64).IsRequired();
        builder.Property(c => c.Position).HasColumnName("position").HasMaxLength(16);
    }
}
