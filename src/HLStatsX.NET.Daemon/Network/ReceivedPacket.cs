namespace HLStatsX.NET.Daemon.Network;

/// <summary>
/// A single raw UDP payload or STDIN line received from a game server.
/// </summary>
/// <param name="Raw">The UTF-8 decoded packet content.</param>
/// <param name="SenderAddr">
/// The sender's <c>"ip:port"</c> string, used to look up the matching
/// <see cref="State.ServerState"/>. In STDIN mode this is <c>"stdin:0"</c>.
/// </param>
public readonly record struct ReceivedPacket(string Raw, string SenderAddr);
