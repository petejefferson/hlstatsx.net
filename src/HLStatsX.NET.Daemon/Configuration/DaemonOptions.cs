namespace HLStatsX.NET.Daemon.Configuration;

public sealed class DaemonOptions
{
    public string BindIp { get; init; } = "";
    public int Port { get; init; } = 27500;
    public int DebugLevel { get; init; } = 1;
    public int EventQueueSize { get; init; } = 10;

    // Populated from hlstats_Options in DB (mirroring Perl $directives_mysql)
    public string Mode { get; set; } = "Normal";          // Normal | LAN | NameTrack
    public bool UseTimestamp { get; set; } = false;       // use log timestamp vs. server clock
    public bool DnsResolveIp { get; set; } = true;
    public int DnsTimeoutSeconds { get; set; } = 5;
    public int SkillMaxChange { get; set; } = 100;
    public int SkillMinChange { get; set; } = 2;
    public bool SkillRatioCap { get; set; } = false;
    public int PlayerMinKills { get; set; } = 50;
    public bool AllowOnlyConfigServers { get; set; } = true;
    public bool TrackStatsTrend { get; set; } = false;
    public bool GlobalBanning { get; set; } = false;
    public bool LogChat { get; set; } = false;
    public bool LogChatAdmins { get; set; } = false;
    public int GlobalChat { get; set; } = 0;              // 0=off, 1=all, 2=admins only
    public string RankingType { get; set; } = "skill";   // skill | kills
    public bool UseGeoIpBinary { get; set; } = false;
    public string GeoIpDatabasePath { get; set; } = "GeoLiteCity/GeoLite2-City.mmdb";
}
