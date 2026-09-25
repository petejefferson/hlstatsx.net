using HLStatsX.NET.Awards.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HLStatsX.NET.Awards.Workers;

// Scheduled maintenance worker.
//
// On-demand (exit after run):
//   --run-now              run default tasks: inactive, awards, ribbons, prune
//   --run-now --inactive   run only: player activity update
//   --run-now --awards     run only: daily/global award calculation
//   --run-now --ribbons    run only: ribbon recalculation
//   --run-now --prune      run only: data pruning
//   --run-now --clans      run only: clan membership recalculation
//   --run-now --optimize   run only: OPTIMIZE TABLE on all tables
//   --run-now --geoip      run only: GeoIP lookup for players with no location
//   --run-now --resolve    run only: reverse DNS resolution for connect events
//   --run-now --all        run all tasks
//
// Multiple flags can be combined: --run-now --awards --ribbons
//
// Preview (no writes):
//   --preview              show daily award winners for yesterday — writes nothing to DB
//   --preview --date yyyy-MM-dd  show daily award winners for a specific date
public class AwardsWorker : BackgroundService
{
    private readonly IPruningService _pruningService;
    private readonly IPlayerActivityService _activityService;
    private readonly IAwardsCalculationService _awardsService;
    private readonly IRibbonsService _ribbonsService;
    private readonly IClansService _clansService;
    private readonly IOptimizeService _optimizeService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDnsResolveService _dnsResolveService;
    private readonly IConfiguration _config;
    private readonly ILogger<AwardsWorker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AwardsWorker(
        IPruningService pruningService,
        IPlayerActivityService activityService,
        IAwardsCalculationService awardsService,
        IRibbonsService ribbonsService,
        IClansService clansService,
        IOptimizeService optimizeService,
        IGeoIpService geoIpService,
        IDnsResolveService dnsResolveService,
        IConfiguration config,
        ILogger<AwardsWorker> logger,
        IHostApplicationLifetime lifetime)
    {
        _pruningService    = pruningService;
        _activityService   = activityService;
        _awardsService     = awardsService;
        _ribbonsService    = ribbonsService;
        _clansService      = clansService;
        _optimizeService   = optimizeService;
        _geoIpService      = geoIpService;
        _dnsResolveService = dnsResolveService;
        _config            = config;
        _logger            = logger;
        _lifetime          = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Contains("--preview"))
        {
            await RunPreviewAsync(args, stoppingToken);
            _lifetime.StopApplication();
            return;
        }

        if (args.Contains("--run-now"))
        {
            var tasks = MaintenanceTaskFlags.FromArgs(args);
            await RunTasksAsync(tasks, stoppingToken);
            _lifetime.StopApplication();
            return;
        }

        var runOnStartup = _config.GetValue<bool>("HLStatsX:Awards:RunOnStartup", false);
        if (runOnStartup)
        {
            await RunTasksAsync(MaintenanceTaskFlags.Default, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogInformation("Next maintenance run scheduled in {Hours:F1} hours", delay.TotalHours);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await RunTasksAsync(MaintenanceTaskFlags.Default, stoppingToken);
        }
    }

    private async Task RunTasksAsync(MaintenanceTaskFlags tasks, CancellationToken ct)
    {
        _logger.LogInformation("Starting maintenance — tasks: {Tasks}", tasks);

        if (tasks.Prune)
        {
            try
            {
                var r = await _pruningService.PruneAsync(ct);
                _logger.LogInformation(
                    "Pruning complete — {EventRows} event rows, {HistoryRows} history rows, " +
                    "{TrendRows} trend rows, {ServerLoadRows} server load rows deleted ({Days} day threshold)",
                    r.EventRowsDeleted, r.HistoryRowsDeleted, r.TrendRowsDeleted, r.ServerLoadRowsDeleted, r.DeleteDays);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Pruning task failed");
            }
        }

        if (tasks.Inactive)
        {
            try
            {
                var r = await _activityService.UpdateAsync(ct);
                _logger.LogInformation("Player activity updated (minActivity: {Days} days, timestampMode: {Mode})",
                    r.MinActivityDays, r.UsedTimestamp);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Player activity task failed");
            }
        }

        if (tasks.Awards)
        {
            try
            {
                var r = await _awardsService.CalculateAsync(ct);
                _logger.LogInformation("Awards calculated — {Count} awards processed for date {Date}",
                    r.AwardsProcessed, r.AwardsDate);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Awards calculation task failed");
            }
        }

        if (tasks.Ribbons)
        {
            try
            {
                var r = await _ribbonsService.RecalculateAsync(ct);
                _logger.LogInformation("Ribbons recalculated — {Games} games, {Ribbons} ribbons, {Players} assignments",
                    r.GamesProcessed, r.RibbonsProcessed, r.PlayersAwarded);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ribbons task failed");
            }
        }

        if (tasks.Clans)
        {
            try
            {
                var r = await _clansService.RecalculateAsync(ct);
                _logger.LogInformation(
                    "Clans recalculated — {Total} players, {Created} clans created, {Updated} assigned, {Cleared} cleared",
                    r.PlayersProcessed, r.ClansCreated, r.PlayersUpdated, r.PlayersCleared);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Clans task failed");
            }
        }

        if (tasks.Optimize)
        {
            try
            {
                var r = await _optimizeService.OptimizeAsync(ct);
                _logger.LogInformation("Optimize complete — {Count} tables", r.TablesOptimized);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Optimize task failed");
            }
        }

        if (tasks.GeoIp)
        {
            try
            {
                var r = await _geoIpService.LookupAsync(ct);
                _logger.LogInformation("GeoIP lookup complete — {Updated} updated, {Skipped} skipped", r.PlayersUpdated, r.PlayersSkipped);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "GeoIP lookup task failed");
            }
        }

        if (tasks.Resolve)
        {
            try
            {
                var r = await _dnsResolveService.ResolveAsync(ct);
                _logger.LogInformation(
                    "DNS resolve complete — {Resolved} resolved, {Failed} failed, {Skipped} skipped",
                    r.IpsResolved, r.IpsFailed, r.IpsSkipped);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DNS resolve task failed");
            }
        }

        _logger.LogInformation("Maintenance complete");
    }

    private async Task RunPreviewAsync(string[] args, CancellationToken ct)
    {
        DateOnly? date = null;
        var dateIdx = Array.IndexOf(args, "--date");
        if (dateIdx >= 0 && dateIdx + 1 < args.Length)
        {
            if (DateOnly.TryParse(args[dateIdx + 1], out var parsed))
                date = parsed;
            else
                _logger.LogWarning("Invalid --date value '{Value}' — using yesterday", args[dateIdx + 1]);
        }

        _logger.LogInformation("Awards preview — querying daily award winners (no data will be written)");

        AwardsPreviewResult result;
        try
        {
            result = await _awardsService.PreviewAsync(date, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Awards preview failed");
            return;
        }

        // Column widths
        const int wGame = 8;
        const int wType = 4;
        const int wCode = 20;
        const int wName = 32;
        const int wId   = 10;
        const int wPlayer = 24;
        const int wCount = 7;

        var header = $"{"Game",-wGame}  {"Type",-wType}  {"Code",-wCode}  {"Award Name",-wName}  {"WinnerId",wId}  {"Winner",-wPlayer}  {"Count",wCount}";
        var separator = new string('-', header.Length);

        Console.WriteLine();
        Console.WriteLine($"=== Awards Preview — date: {result.AwardsDate} ===");
        Console.WriteLine(separator);
        Console.WriteLine(header);
        Console.WriteLine(separator);

        foreach (var row in result.Rows.OrderBy(r => r.AwardName))
        {
            var id     = row.DailyWinnerPlayerId?.ToString() ?? "-";
            var name   = row.DailyWinnerName ?? "-";
            var count  = row.DailyWinnerCount?.ToString() ?? "-";
            Console.WriteLine($"{row.Game,-wGame}  {row.AwardType,-wType}  {row.AwardCode,-wCode}  {row.AwardName,-wName}  {id,wId}  {name,-wPlayer}  {count,wCount}");
        }

        Console.WriteLine(separator);
        Console.WriteLine($"Total: {result.Rows.Count} awards, {result.Rows.Count(r => r.DailyWinnerPlayerId.HasValue)} with winners");
        Console.WriteLine();
    }

    private TimeSpan TimeUntilNextRun()
    {
        var runAtStr = _config["HLStatsX:Awards:RunAt"] ?? "03:00";
        if (!TimeOnly.TryParse(runAtStr, out var runAt))
            runAt = new TimeOnly(3, 0);

        var now = DateTime.Now;
        var target = runAt.ToTimeSpan();
        var current = now.TimeOfDay;

        return current < target
            ? target - current
            : TimeSpan.FromHours(24) - current + target;
    }

}
