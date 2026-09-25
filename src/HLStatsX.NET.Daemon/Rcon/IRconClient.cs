namespace HLStatsX.NET.Daemon.Rcon;

public interface IRconClient : IAsyncDisposable
{
    Task<string?> ExecuteAsync(string command, CancellationToken ct = default);
}
