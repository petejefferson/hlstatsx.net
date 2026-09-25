using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HLStatsX.NET.Daemon.Network;

/// <summary>
/// Receives raw UDP log packets from game servers and exposes them as an async
/// enumerable stream. Optionally reads from STDIN instead (import mode).
/// </summary>
/// <remarks>
/// In normal mode the socket binds to <c>bindIp:port</c> and loops indefinitely.
/// In STDIN mode it reads until EOF, then completes. Both modes decode each
/// packet/line as UTF-8 and yield the raw string alongside the sender's address.
/// </remarks>
public sealed class UdpListener : IDisposable
{
    private const int MaxUdpPayload = 65507;

    private readonly string _bindIp;
    private readonly int _port;
    private readonly bool _stdinMode;
    private readonly string _stdinServerAddress;
    private readonly string _inputFile;
    private readonly ILogger<UdpListener> _logger;

    private UdpClient? _udpClient;

    public UdpListener(string bindIp, int port, bool stdinMode, string stdinServerAddress, string inputFile, ILogger<UdpListener> logger)
    {
        _bindIp             = bindIp;
        _port               = port;
        _stdinMode          = stdinMode;
        _stdinServerAddress = stdinServerAddress;
        _inputFile          = inputFile;
        _logger             = logger;
    }

    public bool IsStdinMode => _stdinMode || !string.IsNullOrEmpty(_inputFile);

    /// <summary>
    /// Starts listening and yields <see cref="ReceivedPacket"/> for each
    /// incoming log line until <paramref name="ct"/> is cancelled.
    /// </summary>
    public async IAsyncEnumerable<ReceivedPacket> ListenAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_inputFile))
        {
            await foreach (var packet in ReadFileAsync(_inputFile, ct))
                yield return packet;
        }
        else if (_stdinMode)
        {
            await foreach (var packet in ReadStdinAsync(ct))
                yield return packet;
        }
        else
        {
            await foreach (var packet in ReadUdpAsync(ct))
                yield return packet;
        }
    }

    private async IAsyncEnumerable<ReceivedPacket> ReadUdpAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var endpoint = string.IsNullOrEmpty(_bindIp) || _bindIp == "0.0.0.0"
            ? new IPEndPoint(IPAddress.Any, _port)
            : new IPEndPoint(IPAddress.Parse(_bindIp), _port);

        _udpClient = new UdpClient(endpoint);
        _logger.LogInformation("UDP listener bound to {Endpoint}.", endpoint);

        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _udpClient.ReceiveAsync(ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP receive error.");
                continue;
            }

            var raw = Encoding.UTF8.GetString(result.Buffer);
            var sender = result.RemoteEndPoint;
            yield return new ReceivedPacket(raw, $"{sender.Address}:{sender.Port}");
        }
    }

    private async IAsyncEnumerable<ReceivedPacket> ReadFileAsync(
        string path,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var addr = string.IsNullOrEmpty(_stdinServerAddress) ? "stdin:0" : _stdinServerAddress;
        _logger.LogInformation("Reading log from file: {Path}", path);
        using var reader = new StreamReader(path, Encoding.UTF8);
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (ct.IsCancellationRequested) yield break;
            yield return new ReceivedPacket(line, addr);
        }
    }

    private async IAsyncEnumerable<ReceivedPacket> ReadStdinAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var addr = string.IsNullOrEmpty(_stdinServerAddress) ? "stdin:0" : _stdinServerAddress;
        string? line;
        while ((line = await Console.In.ReadLineAsync(ct)) is not null)
        {
            if (ct.IsCancellationRequested) yield break;
            yield return new ReceivedPacket(line, addr);
        }
    }

    public void Dispose()
    {
        _udpClient?.Dispose();
    }
}
