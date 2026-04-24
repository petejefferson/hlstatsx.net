using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventPlayerActionConfiguration : IEntityTypeConfiguration<EventPlayerAction>
{
    public void Configure(EntityTypeBuilder<EventPlayerAction> builder)
    {
        builder.ToTable("hlstats_Events_PlayerActions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.ActionId).HasColumnName("actionId");
        builder.Property(e => e.Bonus).HasColumnName("bonus");
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);

        builder.HasIndex(e => e.PlayerId);
        builder.HasIndex(e => e.ActionId);
    }
}

public class EventPlayerPlayerActionConfiguration : IEntityTypeConfiguration<EventPlayerPlayerAction>
{
    public void Configure(EntityTypeBuilder<EventPlayerPlayerAction> builder)
    {
        builder.ToTable("hlstats_Events_PlayerPlayerActions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.VictimId).HasColumnName("victimId");
        builder.Property(e => e.ActionId).HasColumnName("actionId");
        builder.Property(e => e.Bonus).HasColumnName("bonus");
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);

        builder.HasIndex(e => e.PlayerId);
        builder.HasIndex(e => e.VictimId);
        builder.HasIndex(e => e.ActionId);
    }
}
