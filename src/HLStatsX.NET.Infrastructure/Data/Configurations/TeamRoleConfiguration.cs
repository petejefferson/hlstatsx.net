using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("hlstats_Teams");
        builder.HasKey(t => t.TeamId);
        builder.Property(t => t.TeamId).HasColumnName("teamId");
        builder.Property(t => t.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(t => t.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        builder.Property(t => t.PlayerlistBgcolor).HasColumnName("playerlist_bgcolor").HasMaxLength(7);
        builder.Property(t => t.PlayerlistColor).HasColumnName("playerlist_color").HasMaxLength(7);
        builder.Property(t => t.PlayerlistIndex).HasColumnName("playerlist_index");

        builder.HasOne(t => t.GameNavigation)
            .WithMany(g => g.Teams)
            .HasForeignKey(t => t.Game)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("hlstats_Roles");
        builder.HasKey(r => r.RoleId);
        builder.Property(r => r.RoleId).HasColumnName("roleId");
        builder.Property(r => r.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(64).IsRequired();

        builder.HasOne(r => r.GameNavigation)
            .WithMany(g => g.Roles)
            .HasForeignKey(r => r.Game)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
