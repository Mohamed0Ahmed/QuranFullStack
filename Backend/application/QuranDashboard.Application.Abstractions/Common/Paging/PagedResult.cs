namespace QuranDashboard.Application.Abstractions.Common.Paging;

public sealed record PagedResult<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);
