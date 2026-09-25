using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class ServerLoadConfiguration : IEntityTypeConfiguration<ServerLoad>
{
    public void Configure(EntityTypeBuilder<ServerLoad> builder)
    {
        builder.ToTable("hlstats_server_load");
        builder.HasKey(s => new { s.ServerId, s.Timestamp });
        builder.Property(s => s.ServerId).HasColumnName("server_id");
        builder.Property(s => s.Timestamp).HasColumnName("timestamp");
        builder.Property(s => s.ActPlayers).HasColumnName("act_players");
        builder.Property(s => s.MinPlayers).HasColumnName("min_players");
        builder.Property(s => s.MaxPlayers).HasColumnName("max_players");
        builder.Property(s => s.Map).HasColumnName("map").HasMaxLength(64);

        builder.Ignore(s => s.DateTime);

        builder.HasOne(s => s.Server)
            .WithMany()
            .HasForeignKey(s => s.ServerId);
    }
}
