using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class VoiceCommServerConfiguration : IEntityTypeConfiguration<VoiceCommServer>
{
    public void Configure(EntityTypeBuilder<VoiceCommServer> builder)
    {
        builder.ToTable("hlstats_Servers_VoiceComm");
        builder.HasKey(v => v.ServerId);
        builder.Property(v => v.ServerId).HasColumnName("serverId");
        builder.Property(v => v.Name).HasColumnName("name").HasMaxLength(64);
        builder.Property(v => v.Addr).HasColumnName("addr").HasMaxLength(64);
        builder.Property(v => v.Password).HasColumnName("password").HasMaxLength(64);
        builder.Property(v => v.Description).HasColumnName("descr").HasMaxLength(128);
        builder.Property(v => v.QueryPort).HasColumnName("queryPort");
        builder.Property(v => v.UdpPort).HasColumnName("UDPPort");
        builder.Property(v => v.ServerType).HasColumnName("serverType");

        builder.Ignore(v => v.IsTeamspeak);
        builder.Ignore(v => v.TypeName);
        builder.Ignore(v => v.DisplayAddress);
        builder.Ignore(v => v.ConnectUrl);
    }
}
