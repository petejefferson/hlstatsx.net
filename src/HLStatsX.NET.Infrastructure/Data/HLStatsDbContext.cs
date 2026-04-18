using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Entities.Events;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Data;

public class HLStatsDbContext : DbContext
{
    public HLStatsDbContext(DbContextOptions<HLStatsDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerName> PlayerNames => Set<PlayerName>();
    public DbSet<PlayerUniqueId> PlayerUniqueIds => Set<PlayerUniqueId>();
    public DbSet<PlayerHistory> PlayerHistories => Set<PlayerHistory>();
    public DbSet<PlayerAward> PlayerAwards => Set<PlayerAward>();
    public DbSet<PlayerRibbon> PlayerRibbons => Set<PlayerRibbon>();
    public DbSet<Clan> Clans => Set<Clan>();
    public DbSet<ClanTag> ClanTags => Set<ClanTag>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerConfig> ServerConfigs => Set<ServerConfig>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Weapon> Weapons => Set<Weapon>();
    public DbSet<MapCount> MapCounts => Set<MapCount>();
    public DbSet<GameAction> GameActions => Set<GameAction>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<Rank> Ranks => Set<Rank>();
    public DbSet<Ribbon> Ribbons => Set<Ribbon>();
    public DbSet<RibbonTrigger> RibbonTriggers => Set<RibbonTrigger>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Livestat> Livestats => Set<Livestat>();
    public DbSet<ServerLoad> ServerLoads => Set<ServerLoad>();
    public DbSet<Trend> Trends => Set<Trend>();
    public DbSet<EventFrag> EventFrags => Set<EventFrag>();
    public DbSet<EventChat> EventChats => Set<EventChat>();
    public DbSet<EventConnect> EventConnects => Set<EventConnect>();
    public DbSet<EventDisconnect> EventDisconnects => Set<EventDisconnect>();
    public DbSet<EventSuicide> EventSuicides => Set<EventSuicide>();
    public DbSet<EventTeamkill> EventTeamkills => Set<EventTeamkill>();
    public DbSet<EventEntry> EventEntries => Set<EventEntry>();
    public DbSet<EventLatency> EventLatencies => Set<EventLatency>();
    public DbSet<EventChangeTeam> EventChangeTeams => Set<EventChangeTeam>();
    public DbSet<EventChangeRole> EventChangeRoles => Set<EventChangeRole>();
    public DbSet<EventPlayerAction> EventPlayerActions => Set<EventPlayerAction>();
    public DbSet<EventPlayerPlayerAction> EventPlayerPlayerActions => Set<EventPlayerPlayerAction>();
    public DbSet<EventStatsme> EventStatsme => Set<EventStatsme>();
    public DbSet<EventStatsme2> EventStatsme2 => Set<EventStatsme2>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HLStatsDbContext).Assembly);
    }

}
