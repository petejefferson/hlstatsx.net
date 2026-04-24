using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("hlstats_Players");
        builder.HasKey(p => p.PlayerId);
        builder.Property(p => p.PlayerId).HasColumnName("playerId");
        builder.Property(p => p.LastName).HasColumnName("lastName").HasMaxLength(64).IsRequired();
        builder.Property(p => p.FullName).HasColumnName("fullName").HasMaxLength(128);
        builder.Property(p => p.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(p => p.ClanId).HasColumnName("clan");
        builder.Property(p => p.Country).HasColumnName("country").HasMaxLength(64);
        builder.Property(p => p.City).HasColumnName("city").HasMaxLength(64);
        builder.Property(p => p.Flag).HasColumnName("flag").HasMaxLength(16);
        builder.Property(p => p.Email).HasColumnName("email").HasMaxLength(64);
        builder.Property(p => p.Homepage).HasColumnName("homepage").HasMaxLength(64);
        builder.Property(p => p.Skill).HasColumnName("skill").HasDefaultValue(1000);
        builder.Property(p => p.Kills).HasColumnName("kills");
        builder.Property(p => p.Deaths).HasColumnName("deaths");
        builder.Property(p => p.Suicides).HasColumnName("suicides");
        builder.Property(p => p.Headshots).HasColumnName("headshots");
        builder.Property(p => p.Shots).HasColumnName("shots");
        builder.Property(p => p.Hits).HasColumnName("hits");
        builder.Property(p => p.Teamkills).HasColumnName("teamkills");
        builder.Property(p => p.ConnectionTime).HasColumnName("connection_time");
        builder.Property(p => p.LastEvent).HasColumnName("last_event");
        builder.Property(p => p.CreateDate).HasColumnName("createdate");
        builder.Property(p => p.HideRanking).HasColumnName("hideranking");
        builder.Property(p => p.ActivityScore).HasColumnName("activity");
        builder.Property(p => p.GameRank).HasColumnName("game_rank");
        builder.Property(p => p.Lat).HasColumnName("lat");
        builder.Property(p => p.Lng).HasColumnName("lng");
        builder.Property(p => p.KillStreak).HasColumnName("kill_streak");
        builder.Property(p => p.DeathStreak).HasColumnName("death_streak");
        builder.Property(p => p.MmRank).HasColumnName("mmrank");
        builder.Ignore(p => p.KillsPerMinute);

        builder.HasOne(p => p.Clan)
            .WithMany(c => c.Players)
            .HasForeignKey(p => p.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.GameNavigation)
            .WithMany()
            .HasForeignKey(p => p.Game)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.Game, p.Skill });
        builder.HasIndex(p => p.LastName);

        builder.Ignore(p => p.IsHidden);
        builder.Ignore(p => p.KillDeathRatio);
        builder.Ignore(p => p.HeadshotPercent);
        builder.Ignore(p => p.Accuracy);
    }
}

public class PlayerNameConfiguration : IEntityTypeConfiguration<PlayerName>
{
    public void Configure(EntityTypeBuilder<PlayerName> builder)
    {
        builder.ToTable("hlstats_PlayerNames");
        builder.HasKey(n => new { n.PlayerId, n.Name });
        builder.Property(n => n.PlayerId).HasColumnName("playerId");
        builder.Property(n => n.Name).HasColumnName("name").HasMaxLength(64);
        builder.Property(n => n.LastUse).HasColumnName("lastuse");
        builder.Property(n => n.Numuses).HasColumnName("numuses");
        builder.Property(n => n.Kills).HasColumnName("kills");
        builder.Property(n => n.Deaths).HasColumnName("deaths");
        builder.Property(n => n.ConnectionTime).HasColumnName("connection_time");
        builder.Property(n => n.Headshots).HasColumnName("headshots");
        builder.Property(n => n.Suicides).HasColumnName("suicides");
        builder.Property(n => n.Shots).HasColumnName("shots");
        builder.Property(n => n.Hits).HasColumnName("hits");
        builder.Ignore(n => n.KdRatio);
        builder.Ignore(n => n.HsKRatio);
        builder.Ignore(n => n.Accuracy);

        builder.HasOne(n => n.Player).WithMany(p => p.Names).HasForeignKey(n => n.PlayerId);
    }
}

public class PlayerUniqueIdConfiguration : IEntityTypeConfiguration<PlayerUniqueId>
{
    public void Configure(EntityTypeBuilder<PlayerUniqueId> builder)
    {
        builder.ToTable("hlstats_PlayerUniqueIds");
        builder.HasKey(u => new { u.PlayerId, u.UniqueId, u.Game });
        builder.Property(u => u.PlayerId).HasColumnName("playerId");
        builder.Property(u => u.UniqueId).HasColumnName("uniqueId").HasMaxLength(64);
        builder.Property(u => u.Game).HasColumnName("game").HasMaxLength(32);
        builder.Property(u => u.Merge).HasColumnName("merge");

        builder.HasOne(u => u.Player).WithMany(p => p.UniqueIds).HasForeignKey(u => u.PlayerId);
    }
}

public class PlayerHistoryConfiguration : IEntityTypeConfiguration<PlayerHistory>
{
    public void Configure(EntityTypeBuilder<PlayerHistory> builder)
    {
        builder.ToTable("hlstats_Players_History");
        builder.HasKey(h => new { h.PlayerId, h.EventTime, h.Game });
        builder.Property(h => h.PlayerId).HasColumnName("playerId");
        builder.Property(h => h.EventTime).HasColumnName("eventTime");
        builder.Property(h => h.Game).HasColumnName("game").HasMaxLength(32);
        builder.Property(h => h.Skill).HasColumnName("skill");
        builder.Property(h => h.Kills).HasColumnName("kills");
        builder.Property(h => h.Deaths).HasColumnName("deaths");
        builder.Property(h => h.Headshots).HasColumnName("headshots");
        builder.Property(h => h.ConnectionTime).HasColumnName("connection_time");
        builder.Property(h => h.SkillChange).HasColumnName("skill_change");
        builder.Property(h => h.Suicides).HasColumnName("suicides");
        builder.Property(h => h.TeamKills).HasColumnName("teamkills");
        builder.Property(h => h.KillStreak).HasColumnName("kill_streak");

        builder.HasOne(h => h.Player).WithMany(p => p.History).HasForeignKey(h => h.PlayerId);
    }
}

public class PlayerAwardConfiguration : IEntityTypeConfiguration<PlayerAward>
{
    public void Configure(EntityTypeBuilder<PlayerAward> builder)
    {
        builder.ToTable("hlstats_Players_Awards");
        builder.HasKey(a => new { a.AwardTime, a.AwardId, a.PlayerId, a.Game });
        builder.Property(a => a.AwardTime).HasColumnName("awardTime");
        builder.Property(a => a.AwardId).HasColumnName("awardId");
        builder.Property(a => a.PlayerId).HasColumnName("playerId");
        builder.Property(a => a.Count).HasColumnName("count");
        builder.Property(a => a.Game).HasColumnName("game").HasMaxLength(32);

        builder.HasOne(a => a.Player).WithMany(p => p.Awards).HasForeignKey(a => a.PlayerId);
        builder.HasOne(a => a.Award).WithMany(a => a.PlayerAwards).HasForeignKey(a => a.AwardId);
    }
}

public class PlayerRibbonConfiguration : IEntityTypeConfiguration<PlayerRibbon>
{
    public void Configure(EntityTypeBuilder<PlayerRibbon> builder)
    {
        builder.ToTable("hlstats_Players_Ribbons");
        builder.HasKey(r => new { r.PlayerId, r.RibbonId, r.Game });
        builder.Property(r => r.PlayerId).HasColumnName("playerId");
        builder.Property(r => r.RibbonId).HasColumnName("ribbonId");
        builder.Property(r => r.Game).HasColumnName("game").HasMaxLength(32);

        builder.HasOne(r => r.Player).WithMany(p => p.Ribbons).HasForeignKey(r => r.PlayerId);
        builder.HasOne(r => r.Ribbon).WithMany(r => r.PlayerRibbons).HasForeignKey(r => r.RibbonId);
    }
}
