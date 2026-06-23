namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public sealed record GetUniqueWordsPageQuery(
    string? Kind,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
