using System.Net.Sockets;
using System.Text;

namespace HLStatsX.NET.Daemon.Rcon;

/// <summary>
/// Source Engine (HL2/OrangeBox) RCON client over TCP.
/// Implements the Source RCON Protocol as used in TRcon.pm.
/// </summary>
public sealed class SourceRconClient : IRconClient
{
    private const int ServerDataAuth          = 3;
    private const int ServerDataAuthResponse  = 2;
    private const int ServerDataExecCommand   = 2;
    private const int ServerDataResponseValue = 0;
    private const int RefreshSocketLimit      = 100;
    private const int AuthPacketId            = 1;
    private const int SplitEndId             = 2;
    private const int ReceiveTimeoutMs        = 1000;

    private readonly string _host;
    private readonly int _port;
    private readonly string _password;
    private readonly ILogger<SourceRconClient> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private bool _authenticated;
    private int _commandCount;
    private int _nextPacketId = 10;

    public SourceRconClient(string host, int port, string password, ILogger<SourceRconClient> logger)
    {
        _host     = host;
        _port     = port;
        _password = password;
        _logger   = logger;
    }

    public async Task<string?> ExecuteAsync(string command, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await ExecuteInternalAsync(command, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string?> ExecuteInternalAsync(string command, CancellationToken ct)
    {
        // Reconnect every 100 commands (mirrors Perl REFRESH_SOCKET_COUNTER_LIMIT)
        if (_commandCount % RefreshSocketLimit == 0)
            await ConnectAsync(ct);

        if (!_authenticated)
            await AuthenticateAsync(ct);

        if (!_authenticated)
        {
            _logger.LogWarning("RCON authentication failed for {Host}:{Port}.", _host, _port);
            return null;
        }

        try
        {
            int id = NextPacketId();
            await SendPacketAsync(id, ServerDataExecCommand, command, ct);
            await SendPacketAsync(SplitEndId, ServerDataExecCommand, string.Empty, ct);

            var sb = new StringBuilder();
            while (true)
            {
                var (pktId, _, body) = await ReceivePacketAsync(ct);
                if (pktId == SplitEndId || pktId == -1)
                    break;
                sb.Append(body);
            }

            _commandCount++;
            return sb.ToString();
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            _logger.LogDebug(ex, "RCON socket error on {Host}:{Port}, will reconnect.", _host, _port);
            DisposeSocket();
            // Retry once
            try
            {
                await ConnectAsync(ct);
                await AuthenticateAsync(ct);
                if (!_authenticated) return null;

                int id = NextPacketId();
                await SendPacketAsync(id, ServerDataExecCommand, command, ct);
                await SendPacketAsync(SplitEndId, ServerDataExecCommand, string.Empty, ct);

                var sb = new StringBuilder();
                while (true)
                {
                    var (pktId, _, body) = await ReceivePacketAsync(ct);
                    if (pktId == SplitEndId || pktId == -1)
                        break;
                    sb.Append(body);
                }

                _commandCount++;
                return sb.ToString();
            }
            catch (Exception retryEx)
            {
                _logger.LogDebug(retryEx, "RCON retry also failed for {Host}:{Port}.", _host, _port);
                DisposeSocket();
                return null;
            }
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        DisposeSocket();
        try
        {
            _tcp    = new TcpClient();
            _tcp.ReceiveTimeout = ReceiveTimeoutMs;
            _tcp.SendTimeout    = ReceiveTimeoutMs;
            await _tcp.ConnectAsync(_host, _port, ct);
            _stream        = _tcp.GetStream();
            _authenticated = false;
            _commandCount  = 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RCON TCP connect failed for {Host}:{Port}.", _host, _port);
            DisposeSocket();
        }
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        if (_stream is null) return;
        try
        {
            await SendPacketAsync(AuthPacketId, ServerDataAuth, _password, ct);

            // Source sends a junk RESPONSE_VALUE before AUTH_RESPONSE
            var (id1, type1, _) = await ReceivePacketAsync(ct);
            if (type1 == ServerDataResponseValue && id1 == AuthPacketId)
            {
                // Read the actual auth response
                var (id2, _, _) = await ReceivePacketAsync(ct);
                _authenticated = id2 == AuthPacketId;
            }
            else
            {
                _authenticated = id1 == AuthPacketId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RCON auth error for {Host}:{Port}.", _host, _port);
            _authenticated = false;
        }
    }

    private async Task SendPacketAsync(int id, int type, string body, CancellationToken ct)
    {
        if (_stream is null) return;

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        // Packet: [id:4][type:4][body:N][0x00][0x00]
        int payloadSize = 4 + 4 + bodyBytes.Length + 2;
        var buf = new byte[4 + payloadSize];

        WriteInt32(buf, 0, payloadSize);
        WriteInt32(buf, 4, id);
        WriteInt32(buf, 8, type);
        Array.Copy(bodyBytes, 0, buf, 12, bodyBytes.Length);
        buf[12 + bodyBytes.Length]     = 0x00;
        buf[12 + bodyBytes.Length + 1] = 0x00;

        await _stream.WriteAsync(buf, ct);
    }

    private async Task<(int id, int type, string body)> ReceivePacketAsync(CancellationToken ct)
    {
        if (_stream is null) return (-1, -1, string.Empty);

        // Read 4-byte size
        var sizeBuf = new byte[4];
        await ReadExactAsync(_stream, sizeBuf, ct);
        int size = ReadInt32(sizeBuf, 0);
        if (size <= 0 || size > 8192) return (-1, -1, string.Empty);

        var payload = new byte[size];
        await ReadExactAsync(_stream, payload, ct);

        int id   = ReadInt32(payload, 0);
        int type = ReadInt32(payload, 4);

        // Body starts at offset 8, strip trailing two null bytes
        int bodyLen = Math.Max(0, size - 10);
        string body = bodyLen > 0
            ? Encoding.UTF8.GetString(payload, 8, bodyLen)
            : string.Empty;

        return (id, type, body);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buf, CancellationToken ct)
    {
        int read = 0;
        while (read < buf.Length)
        {
            int n = await stream.ReadAsync(buf.AsMemory(read, buf.Length - read), ct);
            if (n == 0) throw new IOException("Connection closed.");
            read += n;
        }
    }

    private int NextPacketId()
    {
        var id = _nextPacketId++;
        if (_nextPacketId > 32767) _nextPacketId = 10;
        return id;
    }

    private void DisposeSocket()
    {
        _stream?.Dispose();
        _tcp?.Dispose();
        _stream        = null;
        _tcp           = null;
        _authenticated = false;
    }

    public ValueTask DisposeAsync()
    {
        _lock.Dispose();
        DisposeSocket();
        return ValueTask.CompletedTask;
    }

    private static void WriteInt32(byte[] buf, int offset, int value)
    {
        buf[offset]     = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)((value >> 8) & 0xFF);
        buf[offset + 2] = (byte)((value >> 16) & 0xFF);
        buf[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static int ReadInt32(byte[] buf, int offset)
        => buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16) | (buf[offset + 3] << 24);
}
