namespace QuranDashboard.Application.Abstractions.Common.Paging;

/// <summary>
/// Minimal reusable paged result contract. Feature 014 is the first read API
/// that needs traditional page metadata; this small shared abstraction avoids
/// per-feature pagination shapes without becoming a dumping ground.
/// </summary>
/// <typeparam name="T">The item type of a single page.</typeparam>
public sealed record PagedResult<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);
