using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed class EfPhraseRepetitionsReader(QuranDashboardDbContext db) : IPhraseRepetitionsReader
{
    public async Task<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>> GetCapabilitiesAsync(
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Unavailable();
        }

        var lengthRows = await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(variant => variant.BuildId == snapshot.ActiveBuildId)
            .GroupBy(variant => new { variant.Mode, variant.WordCount })
            .Select(group => new
            {
                group.Key.Mode,
                group.Key.WordCount,
                MaximumOccurrenceCount = group.Max(variant => variant.OccurrenceCount),
            })
            .ToListAsync(cancellationToken);
        var lengths = lengthRows
            .Select(row => new PhraseLengthRow(
                row.Mode,
                row.WordCount,
                row.MaximumOccurrenceCount))
            .OrderBy(row => row.Mode)
            .ThenBy(row => row.WordCount)
            .ToList();

        var modes = new[] { PhraseTextMode.Simple, PhraseTextMode.Tashkil }
            .Select(mode => CreateModeCapabilities(mode, lengths))
            .ToList();

        var response = new PhraseSearchCapabilitiesResponse(
            snapshot.ActiveBuildId,
            snapshot.ExactReady,
            snapshot.SimilarityReady,
            PhraseTextModeKeys.Simple,
            PhraseSearchPaging.MinimumRepetitionLength,
            PhraseRepetitionSortKeys.Occurrences,
            PhraseSearchPaging.DefaultPageSize,
            PhraseSearchPaging.MaximumPageSize,
            PhraseSearchPaging.MaximumRepetitionPageSize,
            PhraseSimilarityContract.Thresholds.Min(),
            [.. PhraseSimilarityContract.Thresholds],
            modes);

        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success(response);
    }

    public async Task<PhraseSearchReadResult<PhraseRepetitionsPageResponse>> GetRepetitionsAsync(
        PhraseTextMode mode,
        short wordCount,
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

        var variants = db.QuranPhraseVariants
            .AsNoTracking()
            .Where(variant => variant.BuildId == snapshot.ActiveBuildId
                && variant.Mode == mode
                && variant.WordCount == wordCount
                && variant.OccurrenceCount >= 2);

        var totalCount = await variants.CountAsync(cancellationToken);
        var ordered = ApplySort(variants, sort);
        var pageOffset = CalculatePageOffset(page, pageSize);

        IReadOnlyList<PhraseRepetitionListItemDto> items = pageOffset is null
            ? []
            : await ordered
                .Skip(pageOffset.Value)
                .Take(pageSize)
                .Select(variant => new PhraseRepetitionListItemDto(
                    variant.Id,
                    variant.DisplayText,
                    variant.OccurrenceCount,
                    variant.AyahCount,
                    variant.SurahCount,
                    variant.FirstQuranWordId))
                .ToListAsync(cancellationToken);

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

    private static PhraseTextModeCapabilitiesDto CreateModeCapabilities(
        PhraseTextMode mode,
        IReadOnlyList<PhraseLengthRow> lengths)
    {
        var modeLengths = lengths
            .Where(row => row.Mode == mode)
            .OrderBy(row => row.WordCount)
            .ToList();
        var supported = modeLengths
            .Select(row => row.WordCount)
            .ToList();
        var repeated = modeLengths
            .Where(row => row.WordCount >= PhraseSearchPaging.MinimumRepetitionLength
                && row.MaximumOccurrenceCount >= 2)
            .Select(row => row.WordCount)
            .ToList();

        return new PhraseTextModeCapabilitiesDto(
            PhraseTextModeContract.CanonicalKey(mode),
            supported,
            repeated,
            supported.LastOrDefault(),
            repeated.LastOrDefault());
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

    private sealed record PhraseLengthRow(
        PhraseTextMode Mode,
        short WordCount,
        long MaximumOccurrenceCount);

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
