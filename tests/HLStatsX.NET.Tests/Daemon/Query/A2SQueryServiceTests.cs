using System.Text;
using HLStatsX.NET.Daemon.Query;

namespace HLStatsX.NET.Tests.Daemon.Query;

public sealed class A2SQueryServiceTests
{
    private static byte[] BuildA2SResponse(
        string hostname, string map, string gameDir, string gameName,
        int numPlayers, int maxPlayers)
    {
        var buf = new List<byte>();
        buf.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }); // key
        buf.Add(0x49);                                        // type 'I'
        buf.Add(0x11);                                        // netver
        buf.AddRange(Encoding.UTF8.GetBytes(hostname)); buf.Add(0x00);
        buf.AddRange(Encoding.UTF8.GetBytes(map));      buf.Add(0x00);
        buf.AddRange(Encoding.UTF8.GetBytes(gameDir));  buf.Add(0x00);
        buf.AddRange(Encoding.UTF8.GetBytes(gameName)); buf.Add(0x00);
        buf.Add(0x01); buf.Add(0x00);                        // appid (2 bytes)
        buf.Add((byte)numPlayers);
        buf.Add((byte)maxPlayers);
        return buf.ToArray();
    }

    [Fact]
    public void ParseResponse_ReturnsCorrectInfo_WhenValidPayload()
    {
        var data = BuildA2SResponse("My Server", "de_dust2", "cstrike", "Counter-Strike: Source", 12, 32);

        var result = A2SQueryService.ParseResponse(data);

        result.Should().NotBeNull();
        result!.Hostname.Should().Be("My Server");
        result.MapName.Should().Be("de_dust2");
        result.GameDir.Should().Be("cstrike");
        result.GameName.Should().Be("Counter-Strike: Source");
        result.NumPlayers.Should().Be(12);
        result.MaxPlayers.Should().Be(32);
    }

    [Fact]
    public void ParseResponse_ReturnsNull_WhenBufferTooShort()
    {
        var result = A2SQueryService.ParseResponse(new byte[3]);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseResponse_HandlesEmptyStrings()
    {
        var data = BuildA2SResponse(string.Empty, string.Empty, string.Empty, string.Empty, 0, 0);

        var result = A2SQueryService.ParseResponse(data);

        result.Should().NotBeNull();
        result!.Hostname.Should().BeEmpty();
        result.MapName.Should().BeEmpty();
        result.GameDir.Should().BeEmpty();
        result.GameName.Should().BeEmpty();
        result.NumPlayers.Should().Be(0);
        result.MaxPlayers.Should().Be(0);
    }

    [Fact]
    public void ParseResponse_ReturnsCorrectPlayerCounts()
    {
        var data = BuildA2SResponse("Server", "map", "game", "gamename", 200, 255);

        var result = A2SQueryService.ParseResponse(data);

        result.Should().NotBeNull();
        result!.NumPlayers.Should().Be(200);
        result.MaxPlayers.Should().Be(255);
    }

    [Fact]
    public void ParseResponse_UsesUtf8Encoding()
    {
        // "Café" in UTF-8 is 'C','a','f', 0xC3, 0xA9
        var data = BuildA2SResponse("Café", "map", "game", "gamename", 1, 10);

        var result = A2SQueryService.ParseResponse(data);

        result.Should().NotBeNull();
        result!.Hostname.Should().Be("Café");
    }
}
