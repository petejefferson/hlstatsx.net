using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class WeaponConfiguration : IEntityTypeConfiguration<Weapon>
{
    public void Configure(EntityTypeBuilder<Weapon> builder)
    {
        builder.ToTable("hlstats_Weapons");
        builder.HasKey(w => w.WeaponId);
        builder.Property(w => w.WeaponId).HasColumnName("weaponId");
        builder.Property(w => w.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(w => w.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(128);
        builder.Property(w => w.Modifier).HasColumnName("modifier");
        builder.Property(w => w.Kills).HasColumnName("kills");
        builder.Property(w => w.Headshots).HasColumnName("headshots");

        builder.Ignore(w => w.HeadshotPercent);
        builder.HasIndex(w => new { w.Game, w.Code }).IsUnique();

        builder.HasOne(w => w.GameNavigation)
            .WithMany(g => g.Weapons)
            .HasForeignKey(w => w.Game)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
