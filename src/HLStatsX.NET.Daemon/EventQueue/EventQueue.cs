using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.EventQueue;

/// <summary>
/// In-memory buffer that accumulates EF Core entities and batch-inserts them via
/// <see cref="IDbContextFactory{HLStatsDbContext}"/> when the queue exceeds a
/// configurable threshold or is explicitly flushed.
/// </summary>
/// <remarks>
/// Mirrors the Perl daemon's <c>recordEvent</c> / <c>flushEventTable</c> pattern:
/// events are accumulated in a list and flushed as a single <c>AddRange</c> +
/// <c>SaveChangesAsync</c> call, avoiding per-row round trips.
/// <para>
/// Callers should also invoke <see cref="FlushAsync"/> on a periodic timer (30 s)
/// to ensure events are not left in the queue indefinitely during low-traffic periods.
/// </para>
/// </remarks>
/// <typeparam name="T">EF Core entity type to batch-insert.</typeparam>
public sealed class EventQueue<T> where T : class
{
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly Func<HLStatsDbContext, DbSet<T>> _setSelector;
    private readonly int _threshold;
    private readonly ILogger _logger;

    private readonly List<T> _buffer = [];

    /// <param name="dbFactory">Used to open a short-lived context on each flush.</param>
    /// <param name="setSelector">
    /// Selects the correct <see cref="DbSet{T}"/> from the context (e.g.
    /// <c>ctx => ctx.EventsFrags</c>).
    /// </param>
    /// <param name="threshold">
    /// Flush when the buffer exceeds this many items.
    /// Defaults to 10, matching the Perl daemon's <c>g_event_queue_size</c>.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public EventQueue(
        IDbContextFactory<HLStatsDbContext> dbFactory,
        Func<HLStatsDbContext, DbSet<T>> setSelector,
        int threshold = 10,
        ILogger? logger = null)
    {
        _dbFactory   = dbFactory;
        _setSelector = setSelector;
        _threshold   = threshold;
        _logger      = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the queue and flushes if the threshold is exceeded.
    /// </summary>
    public async Task EnqueueAsync(T item, CancellationToken ct = default)
    {
        _buffer.Add(item);

        if (_buffer.Count > _threshold)
            await FlushAsync(ct);
    }

    /// <summary>
    /// Writes all buffered items to the database and clears the buffer.
    /// Safe to call when the buffer is empty.
    /// </summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        if (_buffer.Count == 0) return;

        await using var db = _dbFactory.CreateDbContext();
        _setSelector(db).AddRange(_buffer);
        await db.SaveChangesAsync(ct);

        _logger.LogDebug("Flushed {Count} {Entity} event(s) to database.",
            _buffer.Count, typeof(T).Name);

        _buffer.Clear();
    }

    /// <summary>Number of items currently waiting in the buffer.</summary>
    public int Count => _buffer.Count;
}
