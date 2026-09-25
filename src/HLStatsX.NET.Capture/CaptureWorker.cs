using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace HLStatsX.NET.Capture;

public sealed class CaptureWorker : BackgroundService
{
    private readonly CaptureOptions _options;
    private readonly ILogger<CaptureWorker> _logger;

    public CaptureWorker(IOptions<CaptureOptions> options, ILogger<CaptureWorker> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bindEndpoint = _options.BindAddress == "0.0.0.0" || string.IsNullOrEmpty(_options.BindAddress)
            ? new IPEndPoint(IPAddress.Any, _options.Port)
            : new IPEndPoint(IPAddress.Parse(_options.BindAddress), _options.Port);

        IPEndPoint forwardEndpoint;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(_options.ForwardHost, stoppingToken);
            forwardEndpoint = new IPEndPoint(addresses[0], _options.ForwardPort);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Cannot resolve forward host '{Host}'. Capture service will not start.", _options.ForwardHost);
            return;
        }

        // Ensure the log directory exists
        var logDir = Path.GetDirectoryName(Path.GetFullPath(_options.LogFile));
        if (!string.IsNullOrEmpty(logDir))
            Directory.CreateDirectory(logDir);

        using var receiver = new UdpClient(bindEndpoint);
        using var sender   = new UdpClient();
        await using var writer = new StreamWriter(_options.LogFile, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };

        _logger.LogInformation(
            "Capture listening on {Bind}, forwarding to {Forward}, logging to {Log}",
            bindEndpoint, forwardEndpoint, Path.GetFullPath(_options.LogFile));

        while (!stoppingToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await receiver.ReceiveAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP receive error.");
                continue;
            }

            // Forward raw bytes unchanged so the downstream daemon sees the original packet.
            try
            {
                await sender.SendAsync(result.Buffer, forwardEndpoint, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Forward to {Forward} failed.", forwardEndpoint);
            }

            // Write one line per packet — raw UTF-8 content, trailing whitespace stripped.
            // This format is directly compatible with the daemon's stdin replay mode.
            try
            {
                var line = Encoding.UTF8.GetString(result.Buffer).TrimEnd('\r', '\n', '\0');
                await writer.WriteLineAsync(line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Log write failed.");
            }
        }

        _logger.LogInformation("Capture worker stopped.");
    }
}
