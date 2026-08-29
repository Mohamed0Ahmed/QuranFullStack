using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

internal static class PhraseSimilarityRequestValidation
{
    internal static bool TryMinimumMatchedWords(
        PhraseResolutionReference resolution,
        int? requestedMinimumMatchedWords,
        out short minimumMatchedWords,
        out PhraseRequestInvalidKind invalidKind)
    {
        minimumMatchedWords = default;
        invalidKind = PhraseRequestInvalidKind.MinimumMatchedWords;
        var wordCount = resolution.ExactTokenIds.Count;
        if (wordCount is < PhraseSimilarityContract.MinimumLength
            or > PhraseSearchPaging.MaximumSourceLength)
        {
            invalidKind = PhraseRequestInvalidKind.Length;
            return false;
        }

        var minimumAllowed = PhraseSimilarityContract.MinimumMatchedWords(
            wordCount,
            PhraseSimilarityContract.DefaultThreshold);
        if (requestedMinimumMatchedWords is null
            || requestedMinimumMatchedWords < minimumAllowed
            || requestedMinimumMatchedWords > wordCount)
        {
            return false;
        }

        minimumMatchedWords = checked((short)requestedMinimumMatchedWords.Value);
        return true;
    }

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
