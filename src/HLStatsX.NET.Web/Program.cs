using System.Threading.RateLimiting;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Infrastructure.Data;
using HLStatsX.NET.Infrastructure.Repositories;
using HLStatsX.NET.Infrastructure.Services;
using HLStatsX.NET.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
}

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("Steam", c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ISteamService, SteamService>();

// Database
var connectionString = builder.Configuration.GetConnectionString("HLStats")
    ?? throw new InvalidOperationException("Connection string 'HLStats' not found.");

// AddDbContextFactory registers a singleton IDbContextFactory<HLStatsDbContext>.
// Repositories call _factory.CreateDbContext() per method, giving each query its own
// short-lived context — this is required for concurrent Task.WhenAll calls because
// EF Core DbContext is not thread-safe.
int commandTimeout = builder.Configuration.GetValue<int>("HLStatsX:CommandTimeout", 120);
builder.Services.AddDbContextFactory<HLStatsDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0)),
                     o => o.CommandTimeout(commandTimeout))
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// Repositories
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IPlayerStatsRepository, PlayerStatsRepository>();
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IWeaponRepository, WeaponRepository>();
builder.Services.AddScoped<IMapRepository, MapRepository>();
builder.Services.AddScoped<IHeatmapRepository, HeatmapRepository>();
builder.Services.AddScoped<IAwardRepository, AwardRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IActionRepository, ActionRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// Services
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IClanService, ClanService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IAwardService, AwardService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Cookie auth for admin panel
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithThreadId();

    if (ctx.HostingEnvironment.IsDevelopment())
        lc.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    else
        lc.WriteTo.Console(formatter: new CompactJsonFormatter());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(2),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusPage/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

app.Run();

// Expose for WebApplicationFactory in tests
public partial class Program { }
