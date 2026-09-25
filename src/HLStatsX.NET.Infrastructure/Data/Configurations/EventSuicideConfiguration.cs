using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventSuicideConfiguration : IEntityTypeConfiguration<EventSuicide>
{
    public void Configure(EntityTypeBuilder<EventSuicide> builder)
    {
        builder.ToTable("hlstats_Events_Suicides");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.WeaponCode).HasColumnName("weapon").HasMaxLength(64);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.PosX).HasColumnName("pos_x");
        builder.Property(e => e.PosY).HasColumnName("pos_y");
        builder.Property(e => e.PosZ).HasColumnName("pos_z");

        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.EventTime);
    }
}
