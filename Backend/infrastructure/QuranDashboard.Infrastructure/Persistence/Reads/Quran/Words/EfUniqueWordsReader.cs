using QuranDashboard.Application.Abstractions.Common.Filtering;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

// The list-query building (raw SELECTs, WHERE composition, winner predicates, list sort) lives in the
// EfUniqueWordsReader.List.cs partial — split by size, matching the EfStemsReader partial convention.
public sealed partial class EfUniqueWordsReader(QuranDashboardDbContext db) : IUniqueWordsReader
{
    private const int TotalSurahs = 114;

    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSortSpec sort,
        UniqueWordsCountFilter filter,
        UniqueWordsAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = ArabicSearchQueryNormalizer.Normalize(search);
        var kindKey = kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;

        IQueryable<UniqueWordListRow> rows = kind == UniqueWordKind.Tashkeel
            ? BuildTashkeelQuery(normalizedSearch, filter, association)
            : BuildSimpleQuery(normalizedSearch, filter, association);

        rows = ApplySort(rows, sort);

        var totalCount = await rows.CountAsync(cancellationToken);

        var pageRows = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var uniqueWordIds = pageRows.Select(r => (int?)r.Id).ToList();
        var primaryTypes = await LoadPrimaryWordTypesAsync(kind, uniqueWordIds, cancellationToken);
        var primaryRoots = await LoadPrimaryRootsAsync(kind, uniqueWordIds, cancellationToken);

        var items = pageRows
            .Select(r =>
            {
                var type = primaryTypes.GetValueOrDefault(r.Id);
                var root = primaryRoots.GetValueOrDefault(r.Id);
                return new UniqueWordListItemDto(
                    r.Id,
                    kindKey,
                    r.DisplayText,
                    r.OccurrencesCount,
                    r.AyahsCount,
                    r.SurahsCount,
                    TotalSurahs - r.SurahsCount,
                    type?.Code,
                    type?.BroadArabicLabel,
                    root?.RootId,
                    root?.RootText);
            })
            .ToList();

        return new PagedResult<UniqueWordListItemDto>(page, pageSize, totalCount, items);
    }

    public async Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        var row = kind == UniqueWordKind.Tashkeel
            ? await _db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordSummaryRow(
                    UniqueWordKindKeys.Tashkeel,
                    w.TextUthmani,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount))
                .FirstOrDefaultAsync(cancellationToken)
            : await _db.QuranWordsUniqueSimple
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordSummaryRow(
                    UniqueWordKindKeys.Simple,
                    w.TextUthmaniSimple,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount))
                .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new UniqueWordSummaryDto(
            id,
            row.KindKey,
            row.DisplayText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            TotalSurahs - row.SurahsCount);
    }

    public async Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        if (!await UniqueWordExistsAsync(kind, id, cancellationToken))
        {
            return null;
        }

        var surahGroups = await ReadableMatchesQuery(kind, id)
            .GroupBy(w => w.SurahNumber)
            .Select(g => new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (surahGroups.Count == 0)
        {
            return new UniqueWordSurahsResponse([]);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .OrderBy(r => r.SurahNumber)
            .Select(r => new UniqueWordSurahItemDto(
                r.SurahNumber,
                surahNames[r.SurahNumber],
                r.OccurrencesInSurah))
            .ToList();

        return new UniqueWordSurahsResponse(surahs);
    }

    public async Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        if (!await UniqueWordExistsAsync(kind, id, cancellationToken))
        {
            return null;
        }

        var mentionedSurahNumbers = await ReadableMatchesQuery(kind, id)
            .Select(w => w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new UniqueWordMissingSurahsResponse(missingSurahs);
    }

    public async Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind, int id, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await UniqueWordExistsAsync(kind, id, cancellationToken))
        {
            return null;
        }

        var matchedAyahIds = ReadableMatchesQuery(kind, id).Select(w => w.AyahId).Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);

        var pageAyahs = await (
            from ayah in _db.QuranAyahs.AsNoTracking()
            join surah in _db.QuranSurahs.AsNoTracking()
                on ayah.SurahNumber equals surah.SurahNumber
            where matchedAyahIds.Contains(ayah.Id)
            orderby ayah.SurahNumber, ayah.AyahNumber
            select new AyahMetaRow(
                ayah.Id,
                ayah.VerseKey,
                ayah.AyahNumber,
                surah.NameArabic))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<UniqueWordAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await ReadableMatchesQuery(kind, id)
            .Where(w => ayahIds.Contains(w.AyahId))
            .Select(w => new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.Id).ToList());

        var wordsByAyah = await _db.QuranWords
            .AsNoTracking()
            .Where(w => ayahIds.Contains(w.AyahId))
            .OrderBy(w => w.SurahNumber)
            .ThenBy(w => w.AyahNumber)
            .ThenBy(w => w.WordNumber)
            .Select(w => new AyahWordRow(
                w.AyahId,
                w.Id,
                w.WordNumber,
                w.PageNumber,
                w.TextUthmani,
                w.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(w => w.AyahId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                return new UniqueWordAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNameArabic,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                    words
                        .OrderBy(w => w.WordNumber)
                        .Select(w => new AyahWordForHighlightDto(
                            w.QuranWordId,
                            w.TextUthmani,
                            w.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<UniqueWordAyahMatchDto>(page, pageSize, totalCount, items);
    }

    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadable = words
            .Where(w => !w.IsAyahMarker)
            .OrderBy(w => w.WordNumber)
            .FirstOrDefault();

        if (firstReadable is not null)
        {
            return firstReadable.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }

    private IQueryable<QuranWord> ReadableMatchesQuery(UniqueWordKind kind, int id) =>
        kind == UniqueWordKind.Tashkeel
            ? _db.QuranWords.AsNoTracking()
                .Where(w => !w.IsAyahMarker && w.UniqueTashkeelWordId == id)
            : _db.QuranWords.AsNoTracking()
                .Where(w => !w.IsAyahMarker && w.UniqueSimpleWordId == id);

    private async Task<bool> UniqueWordExistsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken) =>
        kind == UniqueWordKind.Tashkeel
            ? await _db.QuranWordsUniqueTashkeel.AsNoTracking().AnyAsync(w => w.Id == id, cancellationToken)
            : await _db.QuranWordsUniqueSimple.AsNoTracking().AnyAsync(w => w.Id == id, cancellationToken);

    // LOCKSTEP WARNING (Feature 026, US7): this primary-selection rule (occurrence count DESC,
    // earliest quran_word id ASC, POS code ordinal ASC) is REPRODUCED in SQL by
    // PrimaryWordTypeWinnerPredicate in EfUniqueWordsReader.List.cs — the primaryType filter and the
    // displayed chip must never disagree. Any change here MUST be mirrored there (and vice versa);
    // UniqueWordsAssociationFilterTests pins the agreement.
    private async Task<IReadOnlyDictionary<int, PrimaryWordTypeRow>> LoadPrimaryWordTypesAsync(
        UniqueWordKind kind, IReadOnlyList<int?> uniqueWordIds, CancellationToken cancellationToken)
    {
        if (uniqueWordIds.Count == 0)
        {
            return new Dictionary<int, PrimaryWordTypeRow>();
        }

        var candidates = kind == UniqueWordKind.Tashkeel
            ? await (from qw in _db.QuranWords.AsNoTracking()
                     join m in _db.WordMorphologies on qw.Id equals m.QuranWordId
                     join pt in _db.PosTags on m.HeadPos equals pt.Code
                     where qw.UniqueTashkeelWordId != null && uniqueWordIds.Contains(qw.UniqueTashkeelWordId)
                     group new { qw.Id } by new { UniqueId = qw.UniqueTashkeelWordId!.Value, pt.Code, pt.Category } into g
                     select new PrimaryTypeCandidateRow(
                         g.Key.UniqueId,
                         g.Key.Code,
                         g.Key.Category,
                         g.Count(),
                         g.Min(x => x.Id)))
                     .ToListAsync(cancellationToken)
            : await (from qw in _db.QuranWords.AsNoTracking()
                     join m in _db.WordMorphologies on qw.Id equals m.QuranWordId
                     join pt in _db.PosTags on m.HeadPos equals pt.Code
                     where qw.UniqueSimpleWordId != null && uniqueWordIds.Contains(qw.UniqueSimpleWordId)
                     group new { qw.Id } by new { UniqueId = qw.UniqueSimpleWordId!.Value, pt.Code, pt.Category } into g
                     select new PrimaryTypeCandidateRow(
                         g.Key.UniqueId,
                         g.Key.Code,
                         g.Key.Category,
                         g.Count(),
                         g.Min(x => x.Id)))
                     .ToListAsync(cancellationToken);

        return candidates
            .GroupBy(c => c.UniqueId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(c => c.OccurrenceCount)
                    .ThenBy(c => c.EarliestQuranWordId)
                    .ThenBy(c => c.Code, StringComparer.Ordinal)
                    .Select(c => new PrimaryWordTypeRow(c.Code, ResolvePrimaryWordTypeBroadLabel(c.Code, c.Category)))
                    .First());
    }

    // LOCKSTEP WARNING (Feature 026, US7): this primary-selection rule (occurrence count DESC,
    // earliest quran_word id ASC, root id ASC) is REPRODUCED in SQL by PrimaryRootWinnerPredicate in
    // EfUniqueWordsReader.List.cs — the rootId filter and the displayed chip must never disagree. Any
    // change here MUST be mirrored there (and vice versa); UniqueWordsAssociationFilterTests pins the
    // agreement.
    private async Task<IReadOnlyDictionary<int, PrimaryRootRow>> LoadPrimaryRootsAsync(
        UniqueWordKind kind, IReadOnlyList<int?> uniqueWordIds, CancellationToken cancellationToken)
    {
        if (uniqueWordIds.Count == 0)
        {
            return new Dictionary<int, PrimaryRootRow>();
        }

        var candidates = kind == UniqueWordKind.Tashkeel
            ? await (from qw in _db.QuranWords.AsNoTracking()
                     join m in _db.WordMorphologies on qw.Id equals m.QuranWordId
                     join root in _db.QuranRoots on m.RootId equals root.Id
                     where qw.UniqueTashkeelWordId != null && uniqueWordIds.Contains(qw.UniqueTashkeelWordId)
                     group new { qw.Id } by new { UniqueId = qw.UniqueTashkeelWordId!.Value, RootId = root.Id, root.RootText } into g
                     select new PrimaryRootCandidateRow(
                         g.Key.UniqueId,
                         g.Key.RootId,
                         g.Key.RootText,
                         g.Count(),
                         g.Min(x => x.Id)))
                     .ToListAsync(cancellationToken)
            : await (from qw in _db.QuranWords.AsNoTracking()
                     join m in _db.WordMorphologies on qw.Id equals m.QuranWordId
                     join root in _db.QuranRoots on m.RootId equals root.Id
                     where qw.UniqueSimpleWordId != null && uniqueWordIds.Contains(qw.UniqueSimpleWordId)
                     group new { qw.Id } by new { UniqueId = qw.UniqueSimpleWordId!.Value, RootId = root.Id, root.RootText } into g
                     select new PrimaryRootCandidateRow(
                         g.Key.UniqueId,
                         g.Key.RootId,
                         g.Key.RootText,
                         g.Count(),
                         g.Min(x => x.Id)))
                     .ToListAsync(cancellationToken);

        return candidates
            .GroupBy(c => c.UniqueId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(c => c.OccurrenceCount)
                    .ThenBy(c => c.EarliestQuranWordId)
                    .ThenBy(c => c.RootId)
                    .Select(c => new PrimaryRootRow(c.RootId, c.RootText))
                    .First());
    }

    private static string? ResolvePrimaryWordTypeBroadLabel(string code, string? category) => code switch
    {
        "INL" => "حروف مقطّعة",
        _ => category switch
        {
            "noun" => "اسم",
            "verb" => "فعل",
            "particle" => "حرف",
            _ => null,
        },
    };

    private sealed record UniqueWordSummaryRow(
        string KindKey,
        string DisplayText,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        short AyahNumber,
        string SurahNameArabic);

    private sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);

    private sealed record PrimaryTypeCandidateRow(
        int UniqueId,
        string Code,
        string? Category,
        int OccurrenceCount,
        int EarliestQuranWordId);

    private sealed record PrimaryWordTypeRow(string Code, string? BroadArabicLabel);

    private sealed record PrimaryRootCandidateRow(
        int UniqueId,
        int RootId,
        string RootText,
        int OccurrenceCount,
        int EarliestQuranWordId);

    private sealed record PrimaryRootRow(int RootId, string RootText);
}
