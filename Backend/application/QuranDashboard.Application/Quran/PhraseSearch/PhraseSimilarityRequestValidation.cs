using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

internal static class PhraseSimilarityRequestValidation
{
    internal static bool TryPaging(
        int? requestedPage,
        int? requestedPageSize,
        out int page,
        out int pageSize)
    {
        page = requestedPage ?? PhraseSearchPaging.DefaultPage;
        pageSize = requestedPageSize ?? PhraseSearchPaging.DefaultPageSize;
        return page >= PhraseSearchPaging.DefaultPage
            && pageSize > 0
            && pageSize <= PhraseSearchPaging.MaximumPageSize;
    }
}
