namespace HLStatsX.NET.Core.Models;

/// <summary>
/// A page of results from a paged database query.
/// Carry the items for the current page alongside the metadata needed to render pagination controls.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    /// <summary>Total number of pages, calculated from <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Creates a new <see cref="PagedResult{T}"/> from the supplied items and metadata.</summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
        new() { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
}
