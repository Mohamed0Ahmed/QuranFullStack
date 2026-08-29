using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseRepetitionsReader(
    QuranDashboardDbContext db,
    PhraseSearchReadCache cache) : IPhraseRepetitionsReader
{
    private readonly QuranDashboardDbContext db = db;
    private readonly PhraseSearchReadCache cache = cache;

    public async Task<PhraseSearchReadResult<PhraseRepetitionsPageResponse>> GetRepetitionsAsync(
        PhraseTextMode mode,
        short wordCount,
        IReadOnlyList<string> searchTerms,
        PhraseRepetitionSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Unavailable();
        }

        var cacheKey = PhraseSearchCacheKeys.Repetitions(
            snapshot.ActiveBuildId,
            mode,
            wordCount,
            searchTerms,
            sort,
            page,
            pageSize);
        if (cache.TryGet(cacheKey, out PhraseRepetitionsPageResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Success(cached);
        }

        var variants = db.QuranPhraseVariants
            .AsNoTracking()
            .Where(variant => variant.BuildId == snapshot.ActiveBuildId
                && variant.Mode == mode
                && variant.WordCount == wordCount
                && variant.OccurrenceCount >= 2);

        if (searchTerms.Count > 0)
        {
            var canonicalTerms = searchTerms
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var searchTokenIds = await db.QuranPhraseSearchTokens
                .AsNoTracking()
                .Where(token => token.BuildId == snapshot.ActiveBuildId
                    && token.Mode == mode
                    && canonicalTerms.Contains(token.SearchText))
                .Select(token => checked((int)token.Id))
                .ToListAsync(cancellationToken);

            if (searchTokenIds.Count != canonicalTerms.Count)
            {
                variants = variants.Where(_ => false);
            }
            else
            {
                foreach (var searchTokenId in searchTokenIds)
                {
                    variants = variants.Where(variant => variant.SearchTokenIds.Contains(searchTokenId));
                }
            }
        }

        var totalCount = await variants.CountAsync(cancellationToken);
        var ordered = ApplySort(variants, sort);
        var pageOffset = CalculatePageOffset(page, pageSize);

        IReadOnlyList<PhraseRepetitionListItemRow> pageVariants = pageOffset is null
            ? []
            : await ordered
                .Skip(pageOffset.Value)
                .Take(pageSize)
                .Select(variant => new PhraseRepetitionListItemRow(
                    variant.Id,
                    variant.DisplayText,
                    variant.WordCount,
                    variant.OccurrenceCount,
                    variant.AyahCount,
                    variant.SurahCount,
                    variant.FirstQuranWordId))
                .ToListAsync(cancellationToken);
        var simpleDisplayTexts = mode == PhraseTextMode.Simple
            ? await LoadSimpleDisplayTextsAsync(pageVariants, cancellationToken)
            : new Dictionary<long, string>();
        IReadOnlyList<PhraseRepetitionListItemDto> items = pageVariants
            .Select(variant => new PhraseRepetitionListItemDto(
                variant.VariantId,
                simpleDisplayTexts.GetValueOrDefault(variant.VariantId, variant.DisplayText),
                variant.OccurrenceCount,
                variant.AyahCount,
                variant.SurahCount,
                variant.FirstQuranWordId))
            .ToList();

        var response = new PhraseRepetitionsPageResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(mode),
            wordCount,
            PhraseRepetitionSortContract.CanonicalKey(sort),
            page,
            pageSize,
            totalCount,
            items);

        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Success(response);
    }

    public async Task<PhraseSearchReadResult<PhraseOccurrencePageResponse>> GetOccurrencesAsync(
        Guid expectedBuildId,
        long variantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseOccurrencePageResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != expectedBuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseOccurrencePageResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.RepetitionOccurrences(
            snapshot.ActiveBuildId,
            variantId,
            page,
            pageSize);
        if (cache.TryGet(cacheKey, out PhraseOccurrencePageResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseOccurrencePageResponse>.Success(cached);
        }

        var variant = await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(candidate => candidate.BuildId == snapshot.ActiveBuildId
                && candidate.Id == variantId
                && candidate.WordCount >= PhraseSearchPaging.MinimumRepetitionLength
                && candidate.OccurrenceCount >= 2)
            .Select(candidate => new PhraseVariantRow(
                candidate.Id,
                candidate.Mode,
                candidate.WordCount,
                candidate.DisplayText,
                candidate.OccurrenceCount,
                candidate.AyahCount,
                candidate.SurahCount))
            .SingleOrDefaultAsync(cancellationToken);

        if (variant is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseOccurrencePageResponse>.NotFound();
        }

        var totalCount = checked((int)variant.OccurrenceCount);
        var pageOffset = CalculatePageOffset(page, pageSize);
        var occurrenceRows = pageOffset is null
            ? []
            : await (
                from occurrence in db.QuranPhraseOccurrences.AsNoTracking()
                join ayah in db.QuranAyahs.AsNoTracking()
                    on occurrence.AyahId equals ayah.Id
                join surah in db.QuranSurahs.AsNoTracking()
                    on ayah.SurahNumber equals surah.SurahNumber
                where occurrence.BuildId == snapshot.ActiveBuildId
                    && occurrence.VariantId == variantId
                orderby ayah.SurahNumber, ayah.AyahNumber, occurrence.StartWordNumber, occurrence.Id
                select new PhraseOccurrenceRow(
                    occurrence.Id,
                    ayah.Id,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    surah.NameArabic,
                    ayah.AyahNumber,
                    ayah.PageFrom,
                    ayah.PageTo,
                    occurrence.StartWordNumber,
                    occurrence.EndWordNumber))
                .Skip(pageOffset.Value)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        var wordsByAyah = await LoadAyahWordsAsync(occurrenceRows, cancellationToken);
        var items = occurrenceRows
            .Select(occurrence => CreateOccurrence(occurrence, wordsByAyah))
            .ToList();

        var phrase = new PhraseRepetitionDetailDto(
            variant.Id,
            PhraseTextModeContract.CanonicalKey(variant.Mode),
            variant.WordCount,
            variant.DisplayText,
            variant.OccurrenceCount,
            variant.AyahCount,
            variant.SurahCount);
        var response = new PhraseOccurrencePageResponse(
            snapshot.ActiveBuildId,
            phrase,
            page,
            pageSize,
            totalCount,
            items);

        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseOccurrencePageResponse>.Success(response);
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<PhraseAyahWordDto>>> LoadAyahWordsAsync(
        IReadOnlyList<PhraseOccurrenceRow> occurrences,
        CancellationToken cancellationToken)
    {
        var ayahIds = occurrences
            .Select(occurrence => occurrence.AyahId)
            .Distinct()
            .ToList();

        if (ayahIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<PhraseAyahWordDto>>();
        }

        var rows = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new PhraseAyahWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PhraseAyahWordDto>)group
                    .Select(row => new PhraseAyahWordDto(
                        row.QuranWordId,
                        row.WordNumber,
                        row.PageNumber,
                        row.TextUthmani))
                    .ToList());
    }

    private static PhraseOccurrenceDto CreateOccurrence(
        PhraseOccurrenceRow occurrence,
        IReadOnlyDictionary<int, IReadOnlyList<PhraseAyahWordDto>> wordsByAyah)
    {
        var words = wordsByAyah.GetValueOrDefault(occurrence.AyahId, []);
        var queryWordIds = words
            .Where(word => word.WordNumber >= occurrence.StartWordNumber
                && word.WordNumber <= occurrence.EndWordNumber)
            .Select(word => word.QuranWordId)
            .ToList();

        return new PhraseOccurrenceDto(
            occurrence.OccurrenceId,
            occurrence.AyahId,
            occurrence.VerseKey,
            occurrence.SurahNumber,
            occurrence.SurahNameArabic,
            occurrence.AyahNumber,
            occurrence.PageFrom,
            occurrence.PageTo,
            occurrence.StartWordNumber,
            occurrence.EndWordNumber,
            words,
            new PhraseOccurrenceHighlightsDto(queryWordIds));
    }

    private static IOrderedQueryable<QuranPhraseVariant> ApplySort(
        IQueryable<QuranPhraseVariant> variants,
        PhraseRepetitionSort sort) => sort switch
    {
        PhraseRepetitionSort.OccurrencesDescending => variants
            .OrderByDescending(variant => variant.OccurrenceCount)
            .ThenBy(variant => variant.Id),
        PhraseRepetitionSort.OccurrencesAscending => variants
            .OrderBy(variant => variant.OccurrenceCount)
            .ThenBy(variant => variant.Id),
        PhraseRepetitionSort.MushafOrder => variants
            .OrderBy(variant => variant.FirstQuranWordId)
            .ThenBy(variant => variant.Id),
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseRepetitionSort)} value: {sort}."),
    };

    private static int? CalculatePageOffset(int page, int pageSize)
    {
        var offset = ((long)page - 1) * pageSize;
        return offset > int.MaxValue ? null : (int)offset;
    }

    private sealed record PhraseVariantRow(
        long Id,
        PhraseTextMode Mode,
        short WordCount,
        string DisplayText,
        long OccurrenceCount,
        int AyahCount,
        short SurahCount);

    private sealed record PhraseOccurrenceRow(
        long OccurrenceId,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        string SurahNameArabic,
        short AyahNumber,
        short PageFrom,
        short PageTo,
        short StartWordNumber,
        short EndWordNumber);

    private sealed record PhraseAyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani);
}
