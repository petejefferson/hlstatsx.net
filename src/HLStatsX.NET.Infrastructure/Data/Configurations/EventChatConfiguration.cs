using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventChatConfiguration : IEntityTypeConfiguration<EventChat>
{
    public void Configure(EntityTypeBuilder<EventChat> builder)
    {
        builder.ToTable("hlstats_Events_Chat");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(128);
        builder.Property(e => e.MessageMode).HasColumnName("message_mode");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");

        builder.HasOne(e => e.Player).WithMany(p => p.ChatEvents).HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.EventTime);
    }
}

public class EventConnectConfiguration : IEntityTypeConfiguration<EventConnect>
{
    public void Configure(EntityTypeBuilder<EventConnect> builder)
    {
        builder.ToTable("hlstats_Events_Connects");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.IpAddress).HasColumnName("ipAddress").HasMaxLength(32);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.EventTimeDisconnect).HasColumnName("eventTime_Disconnect");

        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventDisconnectConfiguration : IEntityTypeConfiguration<EventDisconnect>
{
    public void Configure(EntityTypeBuilder<EventDisconnect> builder)
    {
        builder.ToTable("hlstats_Events_Disconnects");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.EventTime).HasColumnName("eventTime");

        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
    }
}
