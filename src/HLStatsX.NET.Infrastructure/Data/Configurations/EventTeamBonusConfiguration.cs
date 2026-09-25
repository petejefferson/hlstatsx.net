using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class EventTeamBonusConfiguration : IEntityTypeConfiguration<EventTeamBonus>
{
    public void Configure(EntityTypeBuilder<EventTeamBonus> builder)
    {
        builder.ToTable("hlstats_Events_TeamBonuses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventTime).HasColumnName("eventTime");
        builder.Property(e => e.ServerId).HasColumnName("serverId");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(64);
        builder.Property(e => e.PlayerId).HasColumnName("playerId");
        builder.Property(e => e.ActionId).HasColumnName("actionId");
        builder.Property(e => e.Bonus).HasColumnName("bonus");

        builder.HasIndex(e => e.PlayerId);
    }
}
