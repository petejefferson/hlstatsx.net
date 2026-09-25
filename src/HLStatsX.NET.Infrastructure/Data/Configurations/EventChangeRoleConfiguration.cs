using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventChangeRoleConfiguration : IEntityTypeConfiguration<EventChangeRole>
{
    public void Configure(EntityTypeBuilder<EventChangeRole> builder)
    {
        builder.ToTable("hlstats_Events_ChangeRole");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(64);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");

        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PlayerId);
    }
}
