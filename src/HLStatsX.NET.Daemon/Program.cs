using HLStatsX.NET.Daemon;
using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Daemon.Geo;
using HLStatsX.NET.Daemon.Network;
using HLStatsX.NET.Daemon.Query;
using HLStatsX.NET.Daemon.Rcon;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Daemon.Trend;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;
        var connStr = config.GetConnectionString("HLStats")
            ?? throw new InvalidOperationException("Connection string 'HLStats' is required.");

        // Database
        services.AddDbContextFactory<HLStatsDbContext>(opts =>
            opts.UseMySql(connStr, ServerVersion.AutoDetect(connStr),
                mysql => mysql.SchemaBehavior(MySqlSchemaBehavior.Ignore)));

        // Core daemon services
        services.AddSingleton<DaemonStateManager>();
        services.AddSingleton<ClanService>();
        services.AddSingleton<GeoIpLookup>();
        services.AddSingleton<LivestatService>();
        services.AddSingleton<PlayerDbService>();
        services.AddSingleton<TrendTracker>();
        services.AddSingleton<A2SQueryService>();
        services.AddSingleton<ServerBroadcastService>();

        // UDP listener (config from appsettings.json "Daemon" section)
        services.AddSingleton(sp =>
        {
            var cfg      = sp.GetRequiredService<IConfiguration>();
            var log      = sp.GetRequiredService<ILogger<UdpListener>>();
            var bindIp      = cfg["Daemon:BindAddress"] ?? "0.0.0.0";
            var port        = int.Parse(cfg["Daemon:Port"] ?? "27500");
            var stdin       = bool.Parse(cfg["Daemon:StdinMode"] ?? "false");
            var stdinServer = cfg["Daemon:StdinServerAddress"] ?? string.Empty;
            var inputFile   = cfg["Daemon:InputFile"] ?? string.Empty;
            return new UdpListener(bindIp, port, stdin, stdinServer, inputFile, log);
        });

        // Event handlers (stateless — safe as singletons)
        services.AddSingleton<TeamBonusHandler>();
        services.AddSingleton<PlayerActionHandler>();
        services.AddSingleton<PlayerPlayerActionHandler>();
        services.AddSingleton<ConnectHandler>();
        services.AddSingleton<EnterGameHandler>();
        services.AddSingleton<DisconnectHandler>();
        services.AddSingleton<ChangeTeamHandler>();
        services.AddSingleton<ChangeRoleHandler>();
        services.AddSingleton<ChangeNameHandler>();
        services.AddSingleton<FragHandler>();
        services.AddSingleton<SuicideHandler>();
        services.AddSingleton<ChatHandler>();
        services.AddSingleton<MapChangeHandler>();
        services.AddSingleton<StatsmeHandler>();

        // Event router
        services.AddSingleton<EventRouter>();

        // Hosted service
        services.AddHostedService<DaemonWorker>();
    })
    .Build();

await host.RunAsync();
