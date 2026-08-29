using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextResultsResponse>> GetResultsAsync(
        PhraseContextSelection selection,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.ContextResults(selection, page, pageSize);
        if (cache.TryGet(cacheKey, out PhraseContextResultsResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.Success(cached);
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.InvalidReference();
        }

        var pageOffset = (long)(page - 1) * pageSize;
        var loaded = await ReadOccurrencePageAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            pageOffset,
            pageSize,
            cancellationToken);
        if (loaded.TotalCount == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.InvalidReference();
        }

        var occurrences = await LoadContextOccurrencesAsync(
            loaded.Items,
            selection.Resolution,
            cancellationToken);
        var response = new PhraseContextResultsResponse(
            snapshot.ActiveBuildId,
            page,
            pageSize,
            loaded.TotalCount,
            loaded.TotalAyahCount,
            loaded.TotalCount,
            loaded.Items
                .Select(row => CreateContextOccurrence(occurrences[row.OccurrenceId]))
                .ToList());
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseContextResultsResponse>.Success(response);
    }
}
