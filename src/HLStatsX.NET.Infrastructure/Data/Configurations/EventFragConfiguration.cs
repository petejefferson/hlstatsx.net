using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventFragConfiguration : IEntityTypeConfiguration<EventFrag>
{
    public void Configure(EntityTypeBuilder<EventFrag> builder)
    {
        builder.ToTable("hlstats_Events_Frags");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.KillerId).HasColumnName("killerId");
        builder.Property(e => e.VictimId).HasColumnName("victimId");
        builder.Property(e => e.Weapon).HasColumnName("weapon").HasMaxLength(64);
        builder.Property(e => e.Headshot).HasColumnName("headshot");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.KillerRole).HasColumnName("killerRole").HasMaxLength(64);
        builder.Property(e => e.VictimRole).HasColumnName("victimRole").HasMaxLength(64);
        builder.Property(e => e.WeaponId).HasColumnName("weaponId");
        builder.Property(e => e.MapId).HasColumnName("mapId");

        builder.HasOne(e => e.Killer).WithMany(p => p.KillEvents).HasForeignKey(e => e.KillerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Victim).WithMany(p => p.DeathEvents).HasForeignKey(e => e.VictimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EventTime);
        builder.HasIndex(e => e.KillerId);
        builder.HasIndex(e => e.VictimId);
    }
}
