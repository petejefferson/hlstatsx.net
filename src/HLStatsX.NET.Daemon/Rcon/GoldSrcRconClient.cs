using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace HLStatsX.NET.Daemon.Rcon;

/// <summary>
/// GoldSrc (HL1) RCON client over UDP.
/// Implements the challenge/response RCON protocol as used in BASTARDrcon.pm.
/// </summary>
public sealed class GoldSrcRconClient : IRconClient
{
    private static readonly byte[] ChallengePacket =
        "\xFF\xFF\xFF\xFFchallenge rcon\n\x00"u8.ToArray();

    private readonly string _host;
    private readonly int _port;
    private readonly string _password;
    private readonly ILogger<GoldSrcRconClient> _logger;

    public GoldSrcRconClient(string host, int port, string password, ILogger<GoldSrcRconClient> logger)
    {
        _host     = host;
        _port     = port;
        _password = password;
        _logger   = logger;
    }

    public async Task<string?> ExecuteAsync(string command, CancellationToken ct = default)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = 500;
            udp.Connect(_host, _port);

            // Step 1: get challenge number
            await udp.SendAsync(ChallengePacket, ChallengePacket.Length).WaitAsync(ct);
            var challengeResult = await udp.ReceiveAsync(ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(500), ct);
            var challengeStr = Encoding.Latin1.GetString(challengeResult.Buffer);

            var match = Regex.Match(challengeStr, @"challenge rcon (\d+)");
            if (!match.Success)
            {
                _logger.LogDebug("GoldSrc RCON: no challenge response from {Host}:{Port}.", _host, _port);
                return null;
            }

            string challengeNum = match.Groups[1].Value;

            // Step 2: send RCON command
            string commandPacketStr = $"\xFF\xFF\xFF\xFFrcon {challengeNum} \"{_password}\" {command}\x00";
            var commandPacket = Encoding.Latin1.GetBytes(commandPacketStr);
            await udp.SendAsync(commandPacket, commandPacket.Length).WaitAsync(ct);

            var responseResult = await udp.ReceiveAsync(ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(500), ct);
            var responseStr = Encoding.Latin1.GetString(responseResult.Buffer);

            if (responseStr.Contains("bad rcon_password", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("GoldSrc RCON: bad password for {Host}:{Port}.", _host, _port);
                return null;
            }

            // Strip 5-byte header: \xFF\xFF\xFF\xFF + 'l' or 'n'
            if (responseStr.Length >= 5)
                responseStr = responseStr[5..];

            return responseStr.TrimEnd('\0');
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GoldSrc RCON error for {Host}:{Port}.", _host, _port);
            return null;
        }
    }

    internal static int? ParseChallengeNumber(string response)
    {
        var match = Regex.Match(response, @"challenge rcon (\d+)");
        if (!match.Success) return null;
        return int.Parse(match.Groups[1].Value);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
