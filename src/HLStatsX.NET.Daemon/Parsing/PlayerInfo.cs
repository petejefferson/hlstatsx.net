namespace HLStatsX.NET.Daemon.Parsing;

/// <summary>
/// The player fields decoded from a Half-Life log player-string such as
/// <c>"Name&lt;userid&gt;&lt;steamid&gt;&lt;team&gt;"</c>.
/// All fields are present on a fully-validated player; bots and pre-validated
/// Steam connections may have zero/empty values for some fields.
/// </summary>
public sealed class PlayerInfo
{
    public string Name { get; init; } = string.Empty;
    /// <summary>Numeric in-session user ID assigned by the game server.</summary>
    public int UserId { get; init; }
    /// <summary>
    /// Normalised Steam/unique ID. Bots get a <c>"BOT:&lt;md5&gt;"</c> prefix.
    /// Steam IDs are stored as <c>"Y:Z"</c> after stripping the <c>STEAM_X:</c> universe prefix.
    /// </summary>
    public string UniqueId { get; init; } = string.Empty;
    /// <summary>The raw unique ID from the log, before normalisation (used for kick commands).</summary>
    public string PlainUniqueId { get; init; } = string.Empty;
    public string Team { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsBot { get; init; }
}
