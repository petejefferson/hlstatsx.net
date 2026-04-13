using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class ServerConfigConfiguration : IEntityTypeConfiguration<ServerConfig>
{
    public void Configure(EntityTypeBuilder<ServerConfig> builder)
    {
        builder.ToTable("hlstats_Servers_Config");
        builder.HasKey(s => s.ServerConfigId);
        builder.Property(s => s.ServerConfigId).HasColumnName("serverConfigId");
        builder.Property(s => s.ServerId).HasColumnName("serverId");
        builder.Property(s => s.ConfigKey).HasColumnName("parameter").HasMaxLength(64).IsRequired();
        builder.Property(s => s.ConfigValue).HasColumnName("value").HasMaxLength(255);

        builder.HasOne(s => s.Server).WithOne(sv => sv.Config).HasForeignKey<ServerConfig>(s => s.ServerId).OnDelete(DeleteBehavior.Cascade);
    }
}
