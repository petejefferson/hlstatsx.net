using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.ToTable("hlstats_Servers");
        builder.HasKey(s => s.ServerId);
        builder.Property(s => s.ServerId).HasColumnName("serverId");
        builder.Property(s => s.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(s => s.Address).HasColumnName("address").HasMaxLength(32).IsRequired();
        builder.Property(s => s.Port).HasColumnName("port");
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(s => s.SortOrder).HasColumnName("sortorder");
        builder.Property(s => s.PublicAddress).HasColumnName("publicaddress").HasMaxLength(128);
        builder.Property(s => s.RconPassword).HasColumnName("rcon_password").HasMaxLength(128);
        builder.Property(s => s.ActPlayers).HasColumnName("act_players");
        builder.Property(s => s.MaxPlayers).HasColumnName("max_players");
        builder.Property(s => s.ActMap).HasColumnName("act_map").HasMaxLength(64);
        builder.Property(s => s.LastEvent).HasColumnName("last_event");
        builder.Property(s => s.MapStarted).HasColumnName("map_started");
        builder.Property(s => s.Kills).HasColumnName("kills");
        builder.Property(s => s.Headshots).HasColumnName("headshots");
        builder.Property(s => s.Lat).HasColumnName("lat");
        builder.Property(s => s.Lng).HasColumnName("lng");
        builder.Property(s => s.City).HasColumnName("city").HasMaxLength(64);
        builder.Property(s => s.Country).HasColumnName("country").HasMaxLength(64);

        builder.Ignore(s => s.IsActive);
        builder.Ignore(s => s.DisplayAddress);

        builder.HasOne(s => s.GameNavigation)
            .WithMany(g => g.Servers)
            .HasForeignKey(s => s.Game)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
