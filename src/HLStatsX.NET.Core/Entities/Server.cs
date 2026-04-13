namespace HLStatsX.NET.Core.Entities;

public class Server
{
    public int ServerId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublicAddress { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string RconPassword { get; set; } = string.Empty;
    public int ActPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string ActMap { get; set; } = string.Empty;
    public int LastEvent { get; set; }
    public int Kills { get; set; }
    public int Headshots { get; set; }
    public float? Lat { get; set; }
    public float? Lng { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public int MapStarted { get; set; }

    public bool IsActive => LastEvent > 0;
    public string DisplayAddress => !string.IsNullOrEmpty(PublicAddress) ? PublicAddress : $"{Address}:{Port}";

    public Game? GameNavigation { get; set; }
    public ServerConfig? Config { get; set; }
    public ICollection<Livestat> Livestats { get; set; } = new List<Livestat>();
}
