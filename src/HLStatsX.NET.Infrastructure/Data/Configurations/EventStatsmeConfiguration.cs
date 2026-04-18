using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventStatsmeConfiguration : IEntityTypeConfiguration<EventStatsme>
{
    public void Configure(EntityTypeBuilder<EventStatsme> builder)
    {
        builder.ToTable("hlstats_Events_Statsme");
        builder.HasNoKey();
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("PlayerId");
        builder.Property(e => e.Weapon).HasColumnName("weapon").HasMaxLength(64);
        builder.Property(e => e.Kills).HasColumnName("kills");
        builder.Property(e => e.Hits).HasColumnName("hits");
        builder.Property(e => e.Shots).HasColumnName("shots");
        builder.Property(e => e.Headshots).HasColumnName("headshots");
        builder.Property(e => e.Deaths).HasColumnName("deaths");
        builder.Property(e => e.Damage).HasColumnName("damage");
    }
}
