namespace HLStatsX.NET.Core.Models;

public class PaginationModel
{
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public Func<int, string> BuildUrl { get; init; } = _ => "#";

    public static PaginationModel From<T>(PagedResult<T> result, Func<int, string> buildUrl) =>
        new() { CurrentPage = result.Page, TotalPages = result.TotalPages, BuildUrl = buildUrl };
}
