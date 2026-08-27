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

        var loaded = await LoadPopulationAsync(snapshot, selection.Resolution, cancellationToken);
        var filtered = ApplySelection(loaded.Occurrences, selection);
        if (!loaded.QueryExists || filtered.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextResultsResponse>.InvalidReference();
        }

        var pageOffset = Math.Min((long)(page - 1) * pageSize, filtered.Count);
        var response = new PhraseContextResultsResponse(
            snapshot.ActiveBuildId,
            page,
            pageSize,
            filtered.Count,
            filtered.Skip((int)pageOffset).Take(pageSize).Select(CreateContextOccurrence).ToList());
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseContextResultsResponse>.Success(response);
    }
}
