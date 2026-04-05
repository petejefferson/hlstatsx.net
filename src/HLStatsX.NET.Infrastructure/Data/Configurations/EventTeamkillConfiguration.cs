using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventTeamkillConfiguration : IEntityTypeConfiguration<EventTeamkill>
{
    public void Configure(EntityTypeBuilder<EventTeamkill> builder)
    {
        builder.ToTable("hlstats_Events_Teamkills");
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.KillerId).HasColumnName("killerId");
        builder.Property(e => e.VictimId).HasColumnName("victimId");
        builder.Property(e => e.WeaponCode).HasColumnName("weaponCode").HasMaxLength(64);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.Game).HasColumnName("game").HasMaxLength(32);
        builder.Property(e => e.KillerSkillChange).HasColumnName("killerId_skill");

        builder.HasOne(e => e.Killer).WithMany().HasForeignKey(e => e.KillerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Victim).WithMany().HasForeignKey(e => e.VictimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.EventTime);
    }
}
