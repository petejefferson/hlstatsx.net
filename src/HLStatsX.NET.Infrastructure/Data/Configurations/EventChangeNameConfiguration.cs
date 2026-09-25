using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventChangeNameConfiguration : IEntityTypeConfiguration<EventChangeName>
{
    public void Configure(EntityTypeBuilder<EventChangeName> builder)
    {
        builder.ToTable("hlstats_Events_ChangeName");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.OldName).HasColumnName("oldName").HasMaxLength(64);
        builder.Property(e => e.NewName).HasColumnName("newName").HasMaxLength(64);

        builder.HasIndex(e => e.PlayerId);
    }
}
