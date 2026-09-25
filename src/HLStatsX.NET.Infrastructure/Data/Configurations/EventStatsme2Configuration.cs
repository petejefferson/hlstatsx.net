using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventStatsme2Configuration : IEntityTypeConfiguration<EventStatsme2>
{
    public void Configure(EntityTypeBuilder<EventStatsme2> builder)
    {
        builder.ToTable("hlstats_Events_Statsme2");
        builder.HasNoKey();
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.PlayerId).HasColumnName("PlayerId");
        builder.Property(e => e.Weapon).HasColumnName("weapon").HasMaxLength(64);
        builder.Property(e => e.Head).HasColumnName("head");
        builder.Property(e => e.Chest).HasColumnName("chest");
        builder.Property(e => e.Stomach).HasColumnName("stomach");
        builder.Property(e => e.LeftArm).HasColumnName("leftarm");
        builder.Property(e => e.RightArm).HasColumnName("rightarm");
        builder.Property(e => e.LeftLeg).HasColumnName("leftleg");
        builder.Property(e => e.RightLeg).HasColumnName("rightleg");
    }
}
