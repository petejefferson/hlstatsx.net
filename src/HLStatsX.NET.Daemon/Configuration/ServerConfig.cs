namespace HLStatsX.NET.Daemon.Configuration;

/// <summary>
/// Per-server configuration loaded from <c>hlstats_Servers_Config</c>.
/// Mirrors the defaults set in <c>readDatabaseConfig()</c> in the Perl daemon.
/// </summary>
public sealed class ServerConfig
{
    public int MinPlayers { get; init; } = 6;
    public bool BroadcastEvents { get; init; } = false;
    public bool BroadcastPlayerActions { get; init; } = false;
    public string BroadcastEventsCommand { get; init; } = "say";
    public string BroadcastEventsCommandAnnounce { get; init; } = "say";
    public bool PlayerEvents { get; init; } = true;
    public string PlayerEventsCommand { get; init; } = "say";
    public string PlayerEventsCommandOsd { get; init; } = "";
    public string PlayerEventsCommandHint { get; init; } = "";
    public string PlayerEventsAdminCommand { get; init; } = "";
    public bool ShowStats { get; init; } = true;
    public bool AutoTeamBalance { get; init; } = false;
    public bool AutoBanRetry { get; init; } = false;
    public bool TrackServerLoad { get; init; } = false;
    public int MinimumPlayersRank { get; init; } = 0;
    public string Admins { get; init; } = "";
    public bool SwitchAdmins { get; init; } = false;
    public bool IgnoreBots { get; init; } = true;
    public int SkillMode { get; init; } = 0;
    public int GameType { get; init; } = 0;
    public int BonusRoundTime { get; init; } = 0;
    public bool BonusRoundIgnore { get; init; } = false;
    public string Mod { get; init; } = "";
    public bool EnablePublicCommands { get; init; } = true;
    public bool ConnectAnnounce { get; init; } = true;
    public bool UpdateHostname { get; init; } = false;
    public bool DefaultDisplayEvents { get; init; } = true;
    public int TkPenalty { get; init; } = 50;
    public int SuicidePenalty { get; init; } = 5;
    /// <summary>
    /// Game engine: 1 = GoldSrc/HL1, 2 = Source/HL2 (original), 3 = OrangeBox.
    /// Read from <c>hlstats_Servers_Config</c> key <c>GameEngine</c>.
    /// </summary>
    public int GameEngine { get; init; } = 2;
    /// <summary>RCON password for this server (from <c>hlstats_Servers.rcon_password</c>).</summary>
    public string RconPassword { get; init; } = string.Empty;
    /// <summary>
    /// Minimum player rank required to stay on the server (0 = disabled).
    /// Read from <c>hlstats_Servers_Config</c> key <c>MinimumPlayersRank</c>.
    /// </summary>
    public int MinRank { get; init; } = 0;
}
