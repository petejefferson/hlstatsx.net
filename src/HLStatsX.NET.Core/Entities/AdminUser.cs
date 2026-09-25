namespace HLStatsX.NET.Core.Entities;

public class AdminUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int AccLevel { get; set; }
    public int? LinkedPlayerId { get; set; }
}
