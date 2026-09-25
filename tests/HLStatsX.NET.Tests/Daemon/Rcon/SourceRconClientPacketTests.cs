using System.Text;

namespace HLStatsX.NET.Tests.Daemon.Rcon;

/// <summary>
/// Verifies the Source RCON binary packet format without a network connection.
/// </summary>
public sealed class SourceRconClientPacketTests
{
    private static byte[] BuildRconPacket(int id, int type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        int payloadSize = 4 + 4 + bodyBytes.Length + 2;
        var buf = new byte[4 + payloadSize];
        WriteInt32(buf, 0, payloadSize);
        WriteInt32(buf, 4, id);
        WriteInt32(buf, 8, type);
        Array.Copy(bodyBytes, 0, buf, 12, bodyBytes.Length);
        buf[12 + bodyBytes.Length]     = 0x00;
        buf[12 + bodyBytes.Length + 1] = 0x00;
        return buf;
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

    [Fact]
    public void Packet_SizeField_EqualsPayloadLength()
    {
        var body = "status";
        var packet = BuildRconPacket(1, 2, body);

        int sizeField = ReadInt32(packet, 0);

        // payload = 4 (id) + 4 (type) + 6 (body) + 2 (nulls) = 16
        sizeField.Should().Be(16);
    }

    [Fact]
    public void Packet_IdField_AtOffset4()
    {
        var packet = BuildRconPacket(42, 2, "status");

        int idField = ReadInt32(packet, 4);

        idField.Should().Be(42);
    }

    [Fact]
    public void Packet_TypeField_AtOffset8()
    {
        var packet = BuildRconPacket(1, 2, "status");

        int typeField = ReadInt32(packet, 8);

        typeField.Should().Be(2);
    }

    [Fact]
    public void Packet_BodyIsNullTerminated()
    {
        var body = "hello";
        var packet = BuildRconPacket(1, 2, body);

        packet[12 + body.Length].Should().Be(0x00);
    }

    [Fact]
    public void Packet_SecondNullTerminator_Present()
    {
        var body = "hello";
        var packet = BuildRconPacket(1, 2, body);

        packet[12 + body.Length + 1].Should().Be(0x00);
    }

    [Fact]
    public void Packet_EmptyBody_HasMinimumSize()
    {
        var packet = BuildRconPacket(1, 2, string.Empty);

        // 4 (size field) + 4 (id) + 4 (type) + 0 (body) + 2 (null terminators) = 14
        // But size field value = 4+4+0+2 = 10, and total array = 4 + 10 = 14
        packet.Length.Should().Be(14);
    }
}
