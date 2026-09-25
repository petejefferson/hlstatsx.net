namespace HLStatsX.NET.Core.Models;

/// <summary>
/// View model passed to the <c>_Pagination</c> partial view to render page navigation links.
/// Build one with <see cref="From{T}"/> rather than constructing it manually.
/// </summary>
public class PaginationModel
{
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    /// <summary>Delegate that accepts a page number and returns the URL for that page.</summary>
    public Func<int, string> BuildUrl { get; init; } = _ => "#";

    /// <summary>
    /// Creates a <see cref="PaginationModel"/> from a <see cref="PagedResult{T}"/> and a URL builder delegate.
    /// </summary>
    public static PaginationModel From<T>(PagedResult<T> result, Func<int, string> buildUrl) =>
        new() { CurrentPage = result.Page, TotalPages = result.TotalPages, BuildUrl = buildUrl };
}
