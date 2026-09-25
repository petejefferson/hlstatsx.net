namespace HLStatsX.NET.Core.Entities;

/// <summary>
/// Represents a game server tracked by HLStatsX. Maps to <c>hlstats_Servers</c>.
/// The Perl daemon connects to servers via RCON to collect events in real time.
/// </summary>
public class Server
{
    public int ServerId { get; set; }
    public string Game { get; set; } = string.Empty;
    /// <summary>Internal IP/hostname used by the Perl daemon to connect.</summary>
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Optional public-facing address displayed to users. Falls back to
    /// <c>Address:Port</c> when empty — see <see cref="DisplayAddress"/>.
    /// </summary>
    public string PublicAddress { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    /// <summary>RCON password. Never expose this in views or API responses.</summary>
    public string RconPassword { get; set; } = string.Empty;
    public int ActPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string ActMap { get; set; } = string.Empty;
    /// <summary>Unix timestamp of the last event received from this server.</summary>
    public int LastEvent { get; set; }
    public int Kills { get; set; }
    public int Headshots { get; set; }
    /// <summary>GeoIP-derived latitude of the server's IP address.</summary>
    public float? Lat { get; set; }
    /// <summary>GeoIP-derived longitude of the server's IP address.</summary>
    public float? Lng { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    /// <summary>Unix timestamp of when the current map started.</summary>
    public int MapStarted { get; set; }
    /// <summary>Number of rounds won by team A (CT-side / Allies) on the current map.</summary>
    public int MapCtWins { get; set; }
    /// <summary>Number of rounds won by team B (T-side / Axis) on the current map.</summary>
    public int MapTsWins { get; set; }

    /// <summary><c>true</c> when the server has ever sent at least one event.</summary>
    public bool IsActive => LastEvent > 0;
    /// <summary>
    /// Address shown to users: <see cref="PublicAddress"/> when set,
    /// otherwise <c>"address:port"</c>.
    /// </summary>
    public string DisplayAddress => !string.IsNullOrEmpty(PublicAddress) ? PublicAddress : $"{Address}:{Port}";

    public Game? GameNavigation { get; set; }
    public ServerConfig? Config { get; set; }
    public ICollection<Livestat> Livestats { get; set; } = new List<Livestat>();
}
