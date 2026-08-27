using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

public static class PhraseSearchCacheKeys
{
    public static string Capabilities(Guid buildId) =>
        $"phrase-search:{buildId:N}:capabilities";

    public static string Repetitions(
        Guid buildId,
        PhraseTextMode mode,
        short wordCount,
        PhraseRepetitionSort sort,
        int page,
        int pageSize) =>
        $"phrase-search:{buildId:N}:repetitions:{(short)mode}:{wordCount}:{(short)sort}:p{page}:s{pageSize}";

    public static string RepetitionOccurrences(
        Guid buildId,
        long variantId,
        int page,
        int pageSize) =>
        $"phrase-search:{buildId:N}:repetition-occurrences:{variantId}:p{page}:s{pageSize}";

    public static string Resolution(
        Guid buildId,
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments) =>
        $"phrase-search:{buildId:N}:resolution:{(short)mode}:{Hash(normalizedSegments)}";

    public static string ContextBranches(
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging) =>
        ContextKey(
            selection,
            "branches",
            paging.PreviousOffset,
            paging.FollowingOffset,
            paging.PreviousPageSize,
            paging.FollowingPageSize);

    public static string ContextGroups(
        PhraseContextSelection selection,
        PhraseCursorPage paging) =>
        ContextKey(selection, "groups", paging.Offset, paging.PageSize);

    public static string ContextResults(
        PhraseContextSelection selection,
        int page,
        int pageSize) =>
        ContextKey(selection, "results", page, pageSize);

    public static string ContextOccurrences(
        PhraseFullContextReference context,
        PhraseCursorPage paging) =>
        $"phrase-search:{context.BuildId:N}:context-occurrences:{Hash(
            ContextParts(context).Append(paging.Offset.ToString(CultureInfo.InvariantCulture))
                .Append(paging.PageSize.ToString(CultureInfo.InvariantCulture)))}";

    public static string SimilaritySearch(
        PhraseResolutionReference resolution,
        short minimumMatchedWords,
        int page,
        int pageSize) =>
        $"phrase-search:{resolution.BuildId:N}:similarity-search:{Hash(
            ResolutionParts(resolution)
                .Append(minimumMatchedWords.ToString(CultureInfo.InvariantCulture))
                .Append(page.ToString(CultureInfo.InvariantCulture))
                .Append(pageSize.ToString(CultureInfo.InvariantCulture)))}";

    public static string SimilarityGroups(
        Guid buildId,
        PhraseTextMode mode,
        short wordCount,
        short threshold,
        PhraseSimilaritySort sort,
        int page,
        int pageSize) =>
        $"phrase-search:{buildId:N}:similarity-groups:{(short)mode}:{wordCount}:{threshold}:{(short)sort}:p{page}:s{pageSize}";

    public static string SimilarityMatches(
        Guid buildId,
        long anchorVariantId,
        short threshold,
        int page,
        int pageSize) =>
        $"phrase-search:{buildId:N}:similarity-matches:{anchorVariantId}:{threshold}:p{page}:s{pageSize}";

    public static int PageWeight(int pageSize) => Math.Clamp((pageSize + 24) / 25, 1, 40);

    private static string ContextKey(
        PhraseContextSelection selection,
        string resource,
        params int[] paging) =>
        $"phrase-search:{selection.Resolution.BuildId:N}:context-{resource}:{Hash(
            SelectionParts(selection).Concat(paging.Select(value => value.ToString(CultureInfo.InvariantCulture))))}";

    private static IEnumerable<string> SelectionParts(PhraseContextSelection selection) =>
        ResolutionParts(selection.Resolution)
            .Concat(PathParts(selection.Previous))
            .Concat(PathParts(selection.Following));

    private static IEnumerable<string> ContextParts(PhraseFullContextReference context) =>
        new[]
        {
            ((short)context.Mode).ToString(CultureInfo.InvariantCulture),
            Join(context.QueryExactTokenIds),
            Join(context.PreviousExactTokenIds),
            Join(context.FollowingExactTokenIds),
        };

    private static IEnumerable<string> ResolutionParts(PhraseResolutionReference resolution) =>
        new[]
        {
            ((short)resolution.Mode).ToString(CultureInfo.InvariantCulture),
            Join(resolution.ExactTokenIds),
        };

    private static IEnumerable<string> PathParts(PhrasePathReference? path) => path is null
        ? ["none"]
        :
        [
            path.BuildId.ToString("N", CultureInfo.InvariantCulture),
            ((short)path.Mode).ToString(CultureInfo.InvariantCulture),
            ((short)path.Side).ToString(CultureInfo.InvariantCulture),
            Join(path.QueryExactTokenIds),
            Join(path.SelectedExactTokenIds),
            path.EndsAtBoundary ? "boundary" : "open",
        ];

    private static string Join(IEnumerable<int> values) => string.Join(',', values);

    private static string Hash(IEnumerable<string> parts)
    {
        var payload = JsonSerializer.Serialize(parts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes[..12]).ToLowerInvariant();
    }
}
