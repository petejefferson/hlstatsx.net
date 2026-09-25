namespace HLStatsX.NET.Core.Entities;

public class ServerConfig
{
    public int ServerConfigId { get; set; }
    public int ServerId { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;

    public Server? Server { get; set; }
}
