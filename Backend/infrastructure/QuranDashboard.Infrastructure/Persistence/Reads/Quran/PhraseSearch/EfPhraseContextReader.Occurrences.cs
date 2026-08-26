using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

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
        var loaded = await LoadPopulationAsync(snapshot, resolution, cancellationToken);
        var filtered = ApplySelection(loaded.Occurrences, selection);
        if (!loaded.QueryExists || filtered.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextOccurrencesResponse>.InvalidReference();
        }

        var pageItems = filtered
            .Skip(paging.Offset)
            .Take(paging.PageSize)
            .Select(occurrence => CreateContextOccurrence(occurrence))
            .ToList();
        var representative = filtered[0];
        var contextDto = new PhraseFullContextDto(
            codec.EncodeFullContext(context),
            PhraseTextModeContract.CanonicalKey(context.Mode),
            FullPathTokens(representative, PhraseContextSide.Previous),
            CreateResolvedQuery(resolution, representative).Tokens,
            FullPathTokens(representative, PhraseContextSide.Following),
            filtered.Count);
        var scope = codec.ComputeScope(context);
        var response = new PhraseContextOccurrencesResponse(
            snapshot.ActiveBuildId,
            contextDto,
            filtered.Count,
            CreateNextCursor(
                snapshot.ActiveBuildId,
                PhraseCursorKind.ContextOccurrences,
                paging.Offset,
                paging.PageSize,
                filtered.Count,
                scope),
            pageItems);
        await snapshot.CompleteAsync(cancellationToken);
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
