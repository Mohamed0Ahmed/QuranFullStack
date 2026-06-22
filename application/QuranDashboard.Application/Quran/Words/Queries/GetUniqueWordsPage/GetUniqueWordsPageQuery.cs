namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

/// <summary>
/// Request for a single page of unique Quran words. <see cref="Kind"/> and
/// <see cref="Sort"/> are the raw route/query strings; the handler parses them
/// through <c>UniqueWordKindParser</c>/<c>UniqueWordSortParser</c> and returns a
/// controlled validation outcome for unsupported values.
/// </summary>
/// <param name="Kind">
/// <c>tashkeel</c> or <c>simple</c> route key. <see langword="null"/>/unsupported
/// values map to a validation outcome rather than a default.
/// </param>
/// <param name="Search">Optional Arabic contains query; <see langword="null"/> means no filter.</param>
/// <param name="Sort">
/// <c>mushaf-order</c>, <c>occurrences</c>, or <c>alpha</c>. <see langword="null"/>
/// is treated as the default <c>mushaf-order</c>; an explicit unsupported value
/// is a validation failure.
/// </param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Bounded page size (list default is 50).</param>
public sealed record GetUniqueWordsPageQuery(
    string? Kind,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
