using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class ClanConfiguration : IEntityTypeConfiguration<Clan>
{
    public void Configure(EntityTypeBuilder<Clan> builder)
    {
        builder.ToTable("hlstats_Clans");
        builder.HasKey(c => c.ClanId);
        builder.Property(c => c.ClanId).HasColumnName("clanId");
        builder.Property(c => c.Tag).HasColumnName("tag").HasMaxLength(64).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        builder.Property(c => c.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(c => c.Homepage).HasColumnName("homepage").HasMaxLength(64);
        builder.Property(c => c.MapRegion).HasColumnName("mapregion").HasMaxLength(128);
        builder.Property(c => c.IsHidden).HasColumnName("hidden");

        builder.HasIndex(c => new { c.Game, c.Tag });
    }
}
