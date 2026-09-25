namespace HLStatsX.NET.Core.Entities;

/// <summary>One row in <c>hlstats_Servers_VoiceComm</c>.</summary>
public class VoiceCommServer
{
    public int ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Addr { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Description { get; set; }
    public int QueryPort { get; set; }
    public int UdpPort { get; set; }
    /// <summary>0 = TeamSpeak, 1 = Ventrilo.</summary>
    public int ServerType { get; set; }

    public bool IsTeamspeak => ServerType == 0;
    public string TypeName => IsTeamspeak ? "TeamSpeak" : "Ventrilo";

    /// <summary>Returns the address string used in browser-link URLs (e.g. <c>ts2://host:port</c>).</summary>
    public string DisplayAddress => IsTeamspeak ? $"{Addr}:{UdpPort}" : $"{Addr}:{QueryPort}";

    public string ConnectUrl => IsTeamspeak
        ? $"teamspeak://{Addr}:{UdpPort}/"
        : $"ventrilo://{Addr}:{QueryPort}/";
}
