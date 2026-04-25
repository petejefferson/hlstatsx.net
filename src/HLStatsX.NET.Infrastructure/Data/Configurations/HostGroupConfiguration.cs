using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class HostGroupConfiguration : IEntityTypeConfiguration<HostGroup>
{
    public void Configure(EntityTypeBuilder<HostGroup> builder)
    {
        builder.ToTable("hlstats_HostGroups");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.Pattern).HasColumnName("pattern").HasMaxLength(128).IsRequired();
        builder.Property(h => h.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
    }
}
