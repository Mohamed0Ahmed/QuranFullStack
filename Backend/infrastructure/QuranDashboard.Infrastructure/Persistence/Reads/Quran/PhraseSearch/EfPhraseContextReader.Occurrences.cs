using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextOccurrencesResponse>> GetOccurrencesAsync(
        PhraseFullContextReference context,
        PhraseCursorPage paging,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != context.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.ContextOccurrences(context, paging);
        if (cache.TryGet(cacheKey, out PhraseContextOccurrencesResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.Success(cached);
        }

        var resolution = new PhraseResolutionReference(
            context.BuildId,
            context.Mode,
            context.QueryExactTokenIds);
        var selection = new PhraseContextSelection(
            resolution,
            new PhrasePathReference(
                context.BuildId,
                context.Mode,
                PhraseContextSide.Previous,
                context.QueryExactTokenIds,
                context.PreviousExactTokenIds,
                true),
            new PhrasePathReference(
                context.BuildId,
                context.Mode,
                PhraseContextSide.Following,
                context.QueryExactTokenIds,
                context.FollowingExactTokenIds,
                true));
        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.InvalidReference();
        }

        var loaded = await ReadOccurrencePageAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            paging.Offset,
            paging.PageSize,
            cancellationToken);
        var representativeRow = loaded.Representative;
        if (loaded.TotalCount == 0 || representativeRow is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.InvalidReference();
        }

        var requestedRows = loaded.Items
            .Append(representativeRow)
            .ToList();
        var occurrences = await LoadContextOccurrencesAsync(
            requestedRows,
            resolution,
            cancellationToken);
        var pageItems = loaded.Items
            .Select(row => CreateContextOccurrence(occurrences[row.OccurrenceId]))
            .ToList();
        var representative = occurrences[representativeRow.OccurrenceId];
        var contextDto = new PhraseFullContextDto(
            codec.EncodeFullContext(context),
            PhraseTextModeContract.CanonicalKey(context.Mode),
            FullPathTokens(representative, PhraseContextSide.Previous),
            CreateResolvedQuery(resolution, representative).Tokens,
            FullPathTokens(representative, PhraseContextSide.Following),
            loaded.TotalCount);
        var scope = codec.ComputeScope(context);
        var response = new PhraseContextOccurrencesResponse(
            snapshot.ActiveBuildId,
            contextDto,
            loaded.TotalCount,
            CreateNextCursor(
                snapshot.ActiveBuildId,
                PhraseCursorKind.ContextOccurrences,
                paging.Offset,
                paging.PageSize,
                loaded.TotalCount,
                scope),
            pageItems);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(paging.PageSize));
        return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.Success(response);
    }

    private static PhraseContextOccurrenceDto CreateContextOccurrence(ContextOccurrence occurrence)
    {
        var queryWords = occurrence.Words
            .Skip(occurrence.Row.StartWordNumber - 1)
            .Take(occurrence.Row.EndWordNumber - occurrence.Row.StartWordNumber + 1)
            .ToList();
        var previousWords = occurrence.Words.Take(occurrence.Row.StartWordNumber - 1).ToList();
        var followingWords = occurrence.Words.Skip(occurrence.Row.EndWordNumber).ToList();
        return new PhraseContextOccurrenceDto(
            occurrence.Row.OccurrenceId,
            occurrence.Row.AyahId,
            occurrence.Row.VerseKey,
            occurrence.Row.SurahNumber,
            occurrence.Row.SurahNameArabic,
            occurrence.Row.AyahNumber,
            occurrence.Row.PageFrom,
            occurrence.Row.PageTo,
            occurrence.Row.StartWordNumber,
            occurrence.Row.EndWordNumber,
            occurrence.Words
                .Select(word => new PhraseAyahWordDto(
                    word.QuranWordId,
                    word.WordNumber,
                    word.PageNumber,
                    word.TextUthmani))
                .ToList(),
            new PhraseContextHighlightsDto(
                queryWords.Select(word => word.QuranWordId).ToList(),
                previousWords.Select(word => word.QuranWordId).ToList(),
                followingWords.Select(word => word.QuranWordId).ToList()));
    }
}
