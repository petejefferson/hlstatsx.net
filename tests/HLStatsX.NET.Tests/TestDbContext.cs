using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Tests;

/// <summary>
/// Factory that creates <see cref="TestDbContext"/> instances sharing the same in-memory
/// database. Repository tests seed via a direct <see cref="TestDbContext"/> instance, then
/// exercise the repository through this factory — ensuring both see the same data store.
/// </summary>
public class TestDbContextFactory : IDbContextFactory<HLStatsDbContext>
{
    private readonly DbContextOptions<HLStatsDbContext> _options;

    public TestDbContextFactory(DbContextOptions<HLStatsDbContext> options) => _options = options;

    public HLStatsDbContext CreateDbContext() => new TestDbContext(_options);
}

/// <summary>
/// In-memory test context that skips MySQL-specific Fluent API configurations.
/// Keys and basic relationships are configured here directly.
/// </summary>
public class TestDbContext : HLStatsDbContext
{
    public TestDbContext(DbContextOptions<HLStatsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Do NOT apply MySQL Fluent configurations for InMemory tests.

        // Player
        modelBuilder.Entity<Player>().HasKey(p => p.PlayerId);
        modelBuilder.Entity<Player>().Ignore(p => p.KillDeathRatio);
        modelBuilder.Entity<Player>().Ignore(p => p.HeadshotPercent);
        modelBuilder.Entity<Player>().Ignore(p => p.Accuracy);
        modelBuilder.Entity<Player>().Ignore(p => p.IsHidden);
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Clan).WithMany(c => c.Players).HasForeignKey(p => p.ClanId).OnDelete(DeleteBehavior.SetNull);

        // PlayerName — composite key (PlayerId, Name)
        modelBuilder.Entity<PlayerName>().HasKey(p => new { p.PlayerId, p.Name });
        modelBuilder.Entity<PlayerName>().HasOne(n => n.Player).WithMany(p => p.Names).HasForeignKey(n => n.PlayerId);

        // PlayerUniqueId — composite key (PlayerId, UniqueId, Game)
        modelBuilder.Entity<PlayerUniqueId>().HasKey(p => new { p.PlayerId, p.UniqueId, p.Game });
        modelBuilder.Entity<PlayerUniqueId>().HasOne(u => u.Player).WithMany(p => p.UniqueIds).HasForeignKey(u => u.PlayerId);

        // PlayerHistory — composite key (PlayerId, EventTime, Game)
        modelBuilder.Entity<PlayerHistory>().HasKey(p => new { p.PlayerId, p.EventTime, p.Game });
        modelBuilder.Entity<PlayerHistory>().HasOne(h => h.Player).WithMany(p => p.History).HasForeignKey(h => h.PlayerId);

        // PlayerAward — composite key (AwardId, PlayerId, Game)
        modelBuilder.Entity<PlayerAward>().HasKey(p => new { p.AwardTime, p.AwardId, p.PlayerId, p.Game });
        modelBuilder.Entity<PlayerAward>().HasOne(a => a.Player).WithMany(p => p.Awards).HasForeignKey(a => a.PlayerId);
        modelBuilder.Entity<PlayerAward>().HasOne(a => a.Award).WithMany(a => a.PlayerAwards).HasForeignKey(a => a.AwardId);

        // PlayerRibbon — composite key (PlayerId, RibbonId, Game)
        modelBuilder.Entity<PlayerRibbon>().HasKey(p => new { p.PlayerId, p.RibbonId, p.Game });
        modelBuilder.Entity<PlayerRibbon>().HasOne(r => r.Player).WithMany(p => p.Ribbons).HasForeignKey(r => r.PlayerId);
        modelBuilder.Entity<PlayerRibbon>().HasOne(r => r.Ribbon).WithMany(r => r.PlayerRibbons).HasForeignKey(r => r.RibbonId);

        // Clan
        modelBuilder.Entity<Clan>().HasKey(c => c.ClanId);

        // ClanTag
        modelBuilder.Entity<ClanTag>().HasKey(c => c.Id);

        // Game
        modelBuilder.Entity<Game>().HasKey(g => g.Code);

        // Server
        modelBuilder.Entity<Server>().HasKey(s => s.ServerId);
        modelBuilder.Entity<Server>().Ignore(s => s.IsActive);

        // ServerConfig
        modelBuilder.Entity<ServerConfig>().HasKey(s => s.ServerConfigId);
        modelBuilder.Entity<ServerConfig>().HasOne(c => c.Server).WithOne(s => s.Config).HasForeignKey<ServerConfig>(c => c.ServerId);

        // Team, Role
        modelBuilder.Entity<Team>().HasKey(t => t.TeamId);
        modelBuilder.Entity<Role>().HasKey(r => r.RoleId);

        // Weapon
        modelBuilder.Entity<Weapon>().HasKey(w => w.WeaponId);
        modelBuilder.Entity<Weapon>().Ignore(w => w.HeadshotPercent);

        // MapCount
        modelBuilder.Entity<MapCount>().HasKey(m => m.RowId);

        // GameAction
        modelBuilder.Entity<GameAction>().HasKey(a => a.ActionId);

        // Award
        modelBuilder.Entity<Award>().HasKey(a => a.AwardId);

        // Rank
        modelBuilder.Entity<Rank>().HasKey(r => r.RankId);

        // Ribbon
        modelBuilder.Entity<Ribbon>().HasKey(r => r.RibbonId);

        // RibbonTrigger
        modelBuilder.Entity<RibbonTrigger>().HasKey(r => r.TriggerId);
        modelBuilder.Entity<RibbonTrigger>().HasOne(r => r.Ribbon).WithMany().HasForeignKey(r => r.RibbonId);

        // Country
        modelBuilder.Entity<Country>().HasKey(c => c.CountryId);

        // Option — PK is KeyName (string)
        modelBuilder.Entity<Option>().HasKey(o => o.KeyName);

        // AdminUser — PK is Username (string)
        modelBuilder.Entity<AdminUser>().HasKey(u => u.Username);

        // Livestat — composite key (PlayerId, ServerId)
        modelBuilder.Entity<Livestat>().HasKey(l => new { l.PlayerId, l.ServerId });
        modelBuilder.Entity<Livestat>().HasOne(l => l.Server).WithMany(s => s.Livestats).HasForeignKey(l => l.ServerId);

        // ServerLoad — composite key (ServerId, Timestamp); read-only in tests
        modelBuilder.Entity<ServerLoad>().HasKey(s => new { s.ServerId, s.Timestamp });
        modelBuilder.Entity<ServerLoad>().HasOne(s => s.Server).WithMany().HasForeignKey(s => s.ServerId);

        // Trend — composite key (Timestamp, Game); read-only in tests
        modelBuilder.Entity<Trend>().HasKey(t => new { t.Timestamp, t.Game });

        // Event entities not already covered above
        modelBuilder.Entity<EventEntry>().HasKey(e => e.Id);
        modelBuilder.Entity<EventEntry>().HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId);
        modelBuilder.Entity<EventEntry>().HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId);

        modelBuilder.Entity<EventLatency>().HasKey(e => e.Id);
        modelBuilder.Entity<EventLatency>().HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId);

        modelBuilder.Entity<EventChangeTeam>().HasKey(e => e.Id);
        modelBuilder.Entity<EventChangeTeam>().HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId);

        modelBuilder.Entity<EventChangeRole>().HasKey(e => e.Id);
        modelBuilder.Entity<EventChangeRole>().HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId);

        modelBuilder.Entity<EventPlayerAction>().HasKey(e => e.Id);
        modelBuilder.Entity<EventPlayerPlayerAction>().HasKey(e => e.Id);

        // Events
        modelBuilder.Entity<EventFrag>().HasKey(e => e.Id);
        modelBuilder.Entity<EventFrag>()
            .HasOne(e => e.Killer).WithMany(p => p.KillEvents).HasForeignKey(e => e.KillerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventFrag>()
            .HasOne(e => e.Victim).WithMany(p => p.DeathEvents).HasForeignKey(e => e.VictimId).OnDelete(DeleteBehavior.ClientSetNull);
        modelBuilder.Entity<EventFrag>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventChat>().HasKey(e => e.Id);
        modelBuilder.Entity<EventChat>()
            .HasOne(e => e.Player).WithMany(p => p.ChatEvents).HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventChat>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventConnect>().HasKey(e => e.Id);
        modelBuilder.Entity<EventConnect>()
            .HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventConnect>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventDisconnect>().HasKey(e => e.Id);
        modelBuilder.Entity<EventDisconnect>()
            .HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventDisconnect>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventSuicide>().HasKey(e => e.EventId);
        modelBuilder.Entity<EventSuicide>()
            .HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventSuicide>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventTeamkill>().HasKey(e => e.EventId);
        modelBuilder.Entity<EventTeamkill>()
            .HasOne(e => e.Killer).WithMany().HasForeignKey(e => e.KillerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventTeamkill>()
            .HasOne(e => e.Victim).WithMany().HasForeignKey(e => e.VictimId).OnDelete(DeleteBehavior.ClientSetNull);
        modelBuilder.Entity<EventTeamkill>()
            .HasOne(e => e.Server).WithMany().HasForeignKey(e => e.ServerId).OnDelete(DeleteBehavior.Cascade);
    }
}
