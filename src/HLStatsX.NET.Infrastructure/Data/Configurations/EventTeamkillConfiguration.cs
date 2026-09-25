using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventTeamkillConfiguration : IEntityTypeConfiguration<EventTeamkill>
{
    public void Configure(EntityTypeBuilder<EventTeamkill> builder)
    {
        builder.ToTable("hlstats_Events_Teamkills");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.KillerId).HasColumnName("killerId");
        builder.Property(e => e.VictimId).HasColumnName("victimId");
        builder.Property(e => e.WeaponCode).HasColumnName("weapon").HasMaxLength(64);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.PosX).HasColumnName("pos_x");
        builder.Property(e => e.PosY).HasColumnName("pos_y");
        builder.Property(e => e.PosZ).HasColumnName("pos_z");
        builder.Property(e => e.PosVictimX).HasColumnName("pos_victim_x");
        builder.Property(e => e.PosVictimY).HasColumnName("pos_victim_y");
        builder.Property(e => e.PosVictimZ).HasColumnName("pos_victim_z");

        builder.HasOne(e => e.Killer).WithMany().HasForeignKey(e => e.KillerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Victim).WithMany().HasForeignKey(e => e.VictimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.EventTime);
    }
}
