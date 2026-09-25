using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class OptionConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.ToTable("hlstats_Options");
        builder.HasKey(o => o.KeyName);
        builder.Property(o => o.KeyName).HasColumnName("keyname").HasMaxLength(32);
        builder.Property(o => o.Value).HasColumnName("value").HasMaxLength(128);
        builder.Property(o => o.OptType).HasColumnName("opttype");
    }
}

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("hlstats_Users");
        builder.HasKey(u => u.Username);
        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(16);
        builder.Property(u => u.Password).HasColumnName("password").HasMaxLength(32);
        builder.Property(u => u.AccLevel).HasColumnName("acclevel");
        builder.Property(u => u.LinkedPlayerId).HasColumnName("playerId");
    }
}

public class LivestatConfiguration : IEntityTypeConfiguration<Livestat>
{
    public void Configure(EntityTypeBuilder<Livestat> builder)
    {
        builder.ToTable("hlstats_Livestats");
        builder.HasKey(l => new { l.PlayerId, l.ServerId });
        builder.Property(l => l.PlayerId).HasColumnName("player_id");
        builder.Property(l => l.ServerId).HasColumnName("server_id");
        builder.Property(l => l.CliAddress).HasColumnName("cli_address").HasMaxLength(32);
        builder.Property(l => l.CliCity).HasColumnName("cli_city").HasMaxLength(64);
        builder.Property(l => l.CliCountry).HasColumnName("cli_country").HasMaxLength(64);
        builder.Property(l => l.CliFlag).HasColumnName("cli_flag").HasMaxLength(16);
        builder.Property(l => l.SteamId).HasColumnName("steam_id").HasMaxLength(64);
        builder.Property(l => l.Name).HasColumnName("name").HasMaxLength(64);
        builder.Property(l => l.Team).HasColumnName("team").HasMaxLength(64);
        builder.Property(l => l.Kills).HasColumnName("kills");
        builder.Property(l => l.Deaths).HasColumnName("deaths");
        builder.Property(l => l.Suicides).HasColumnName("suicides");
        builder.Property(l => l.Headshots).HasColumnName("headshots");
        builder.Property(l => l.Shots).HasColumnName("shots");
        builder.Property(l => l.Hits).HasColumnName("hits");
        builder.Property(l => l.Ping).HasColumnName("ping");
        builder.Property(l => l.Connected).HasColumnName("connected");
        builder.Property(l => l.SkillChange).HasColumnName("skill_change");
        builder.Property(l => l.Skill).HasColumnName("skill");
        builder.Property(l => l.IsDead).HasColumnName("is_dead");

        builder.Ignore(l => l.HeadshotPercent);
        builder.Ignore(l => l.Accuracy);
        builder.Ignore(l => l.ConnectedFormatted);

        builder.HasOne(l => l.Server).WithMany(s => s.Livestats).HasForeignKey(l => l.ServerId);
    }
}
