using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("hlstats_Countries");
        builder.HasKey(c => c.Flag);
        builder.Property(c => c.Flag).HasColumnName("flag").HasMaxLength(6);
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
    }
}
