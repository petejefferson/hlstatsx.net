using HLStatsX.NET.Capture;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<CaptureOptions>(ctx.Configuration.GetSection("Capture"));
        services.AddHostedService<CaptureWorker>();
    })
    .Build();

await host.RunAsync();
