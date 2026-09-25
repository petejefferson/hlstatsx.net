using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HLStatsX.NET.Daemon.Query;

public sealed record A2SServerInfo(
    string Hostname,
    string MapName,
    string GameDir,
    string GameName,
    int NumPlayers,
    int MaxPlayers);

/// <summary>
/// Queries a Source Engine game server using the A2S_INFO protocol.
/// Used as a fallback when RCON is unavailable to retrieve the current map name.
/// Mirrors the Perl daemon's <c>queryServer</c> call in <c>HLstats_Server.pm</c>.
/// </summary>
public sealed class A2SQueryService
{
    private static readonly byte[] QueryPacket =
        "\xFF\xFF\xFF\xFFTSource Engine Query\x00"u8.ToArray();

    public async Task<A2SServerInfo?> QueryAsync(string host, int port, CancellationToken ct = default)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = 2000;
            await udp.SendAsync(QueryPacket, QueryPacket.Length, host, port).WaitAsync(ct);

            var receiveTask = udp.ReceiveAsync(ct).AsTask();
            if (!receiveTask.Wait(TimeSpan.FromSeconds(2)))
                return null;

            var result = receiveTask.Result;
            return ParseResponse(result.Buffer);
        }
        catch
        {
            return null;
        }
    }

    internal static A2SServerInfo? ParseResponse(byte[] data)
    {
        try
        {
            // Skip: 4-byte key + type byte + netver byte = 6 bytes
            int offset = 6;
            var hostname  = ReadNullTerminatedString(data, ref offset);
            var mapName   = ReadNullTerminatedString(data, ref offset);
            var gameDir   = ReadNullTerminatedString(data, ref offset);
            var gameName  = ReadNullTerminatedString(data, ref offset);
            // Skip 2-byte appid
            offset += 2;
            int numPlayers = data[offset++];
            int maxPlayers = data[offset++];
            return new A2SServerInfo(hostname, mapName, gameDir, gameName, numPlayers, maxPlayers);
        }
        catch
        {
            return null;
        }
    }

    private static string ReadNullTerminatedString(byte[] data, ref int offset)
    {
        int start = offset;
        while (offset < data.Length && data[offset] != 0x00)
            offset++;
        var str = Encoding.UTF8.GetString(data, start, offset - start);
        offset++; // skip null terminator
        return str;
    }
}
