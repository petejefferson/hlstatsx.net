using HLStatsX.NET.Awards.Services;
using HLStatsX.NET.Awards.Workers;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("HLStats")
    ?? throw new InvalidOperationException("Connection string 'HLStats' not found.");

int commandTimeout = builder.Configuration.GetValue<int>("HLStatsX:CommandTimeout", 120);
builder.Services.AddDbContextFactory<HLStatsDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                     o => o.CommandTimeout(commandTimeout))
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddSingleton<IPruningService, PruningService>();
builder.Services.AddSingleton<IPlayerActivityService, PlayerActivityService>();
builder.Services.AddSingleton<IAwardsCalculationService, AwardsCalculationService>();
builder.Services.AddSingleton<IRibbonsService, RibbonsService>();
builder.Services.AddSingleton<IClansService, ClansService>();
builder.Services.AddSingleton<IOptimizeService, OptimizeService>();
builder.Services.AddSingleton<IGeoIpService, GeoIpService>();
builder.Services.AddSingleton<IDnsResolveService, DnsResolveService>();
builder.Services.AddHostedService<AwardsWorker>();

var host = builder.Build();
host.Run();
