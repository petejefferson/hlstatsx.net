using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventLatencyConfiguration : IEntityTypeConfiguration<EventLatency>
{
    public void Configure(EntityTypeBuilder<EventLatency> builder)
    {
        builder.ToTable("hlstats_Events_Latency");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.Ping).HasColumnName("ping");
        builder.Property(e => e.EventTime).HasColumnName("eventTime");

        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PlayerId);
    }
}
