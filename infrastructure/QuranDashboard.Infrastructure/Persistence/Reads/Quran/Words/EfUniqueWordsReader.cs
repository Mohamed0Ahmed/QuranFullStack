using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

public sealed class EfUniqueWordsReader(QuranDashboardDbContext db) : IUniqueWordsReader
{
    private const int TotalSurahs = 114;

    private const string FoldFrom = "أإآٱؤئةىي";
    private const string FoldTo = "ااااواهيي";

    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeArabicQuery(search);
        var kindKey = kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;

        IQueryable<UniqueWordListRow> rows = kind == UniqueWordKind.Tashkeel
            ? BuildTashkeelQuery(normalizedSearch)
            : BuildSimpleQuery(normalizedSearch);

        rows = ApplySort(rows, sort);

        var totalCount = await rows.CountAsync(cancellationToken);

        var pageRows = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pageRows
            .Select(r => new UniqueWordListItemDto(
                r.Id,
                kindKey,
                r.DisplayTextUthmani,
                r.TextUthmani,
                r.TextUthmaniSimple,
                r.TextImlaeiSimple,
                r.WordKeyImlaeiSimple,
                r.QpcGlyph,
                r.OccurrencesCount,
                r.AyahsCount,
                r.SurahsCount,
                TotalSurahs - r.SurahsCount,
                $"{r.FirstSurahNumber}:{r.FirstAyahNumber}",
                r.FirstLocation))
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
                    w.TextUthmani,
                    w.TextUthmani,
                    w.TextUthmaniSimple,
                    w.TextImlaeiSimple,
                    null,
                    null,
                    UniqueWordKindKeys.Tashkeel,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount,
                    w.FirstSurahNumber,
                    w.FirstAyahNumber,
                    w.FirstLocation))
                .FirstOrDefaultAsync(cancellationToken)
            : await _db.QuranWordsUniqueSimple
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordSummaryRow(
                    w.TextUthmani,
                    w.TextUthmani,
                    w.TextUthmaniSimple,
                    w.TextImlaeiSimple,
                    w.WordKeyImlaeiSimple,
                    w.QpcGlyph,
                    UniqueWordKindKeys.Simple,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount,
                    w.FirstSurahNumber,
                    w.FirstAyahNumber,
                    w.FirstLocation))
                .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new UniqueWordSummaryDto(
            id,
            row.KindKey,
            row.DisplayTextUthmani,
            row.TextUthmani,
            row.TextUthmaniSimple,
            row.TextImlaeiSimple,
            row.WordKeyImlaeiSimple,
            row.QpcGlyph,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            TotalSurahs - row.SurahsCount,
            $"{row.FirstSurahNumber}:{row.FirstAyahNumber}",
            row.FirstLocation);
    }

    public async Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var surahGroups = await ReadableMatchesQuery(kind, id)
            .GroupBy(w => w.SurahNumber)
            .Select(g => new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        surahGroups = surahGroups.OrderBy(r => r.SurahNumber).ToList();

        if (surahGroups.Count == 0)
        {
            return new UniqueWordSurahsResponse(
                id,
                header.KindKey,
                header.DisplayTextUthmani,
                0,
                []);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new UniqueWordSurahItemDto(
                r.SurahNumber,
                surahNames[r.SurahNumber],
                r.OccurrencesInSurah))
            .ToList();

        return new UniqueWordSurahsResponse(
            id,
            header.KindKey,
            header.DisplayTextUthmani,
            surahs.Count,
            surahs);
    }

    public async Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
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

        return new UniqueWordMissingSurahsResponse(
            id,
            header.KindKey,
            header.DisplayTextUthmani,
            missingSurahs.Count,
            missingSurahs);
    }

    public async Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind, int id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
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
                ayah.SurahNumber,
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
                    ayah.SurahNumber,
                    ayah.SurahNameArabic,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                    words.Select(w => new AyahWordForHighlightDto(
                        w.QuranWordId,
                        w.WordNumber,
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

    private async Task<UniqueWordHeaderRow?> LoadUniqueWordHeaderAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken)
    {
        if (kind == UniqueWordKind.Tashkeel)
        {
            var row = await _db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordHeaderRow(
                    w.TextUthmani,
                    UniqueWordKindKeys.Tashkeel))
                .FirstOrDefaultAsync(cancellationToken);

            return row;
        }

        var simpleRow = await _db.QuranWordsUniqueSimple
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new UniqueWordHeaderRow(
                w.TextUthmani,
                UniqueWordKindKeys.Simple))
            .FirstOrDefaultAsync(cancellationToken);

        return simpleRow;
    }

    private IQueryable<UniqueWordListRow> BuildTashkeelQuery(string? normalizedSearch)
    {

        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani AS "{nameof(UniqueWordListRow.DisplayTextUthmani)}",
                text_uthmani AS "{nameof(UniqueWordListRow.TextUthmani)}",
                text_uthmani_simple AS "{nameof(UniqueWordListRow.TextUthmaniSimple)}",
                text_imlaei_simple AS "{nameof(UniqueWordListRow.TextImlaeiSimple)}",
                NULL::text AS "{nameof(UniqueWordListRow.WordKeyImlaeiSimple)}",
                NULL::text AS "{nameof(UniqueWordListRow.QpcGlyph)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_surah_number AS "{nameof(UniqueWordListRow.FirstSurahNumber)}",
                first_ayah_number AS "{nameof(UniqueWordListRow.FirstAyahNumber)}",
                first_location AS "{nameof(UniqueWordListRow.FirstLocation)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                text_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_tashkeel
            """;

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{EscapeLikePattern(normalizedSearch)}%";
            sql += " WHERE translate(lower(text_uthmani_simple), @foldFrom, @foldTo) ILIKE @pattern"
                + " OR translate(lower(text_imlaei_simple), @foldFrom, @foldTo) ILIKE @pattern";
            return _db.Database.SqlQueryRaw<UniqueWordListRow>(
                sql,
                new NpgsqlParameter("foldFrom", FoldFrom),
                new NpgsqlParameter("foldTo", FoldTo),
                new NpgsqlParameter("pattern", pattern));
        }

        return _db.Database.SqlQueryRaw<UniqueWordListRow>(sql);
    }

    private IQueryable<UniqueWordListRow> BuildSimpleQuery(string? normalizedSearch)
    {
        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani AS "{nameof(UniqueWordListRow.DisplayTextUthmani)}",
                text_uthmani AS "{nameof(UniqueWordListRow.TextUthmani)}",
                text_uthmani_simple AS "{nameof(UniqueWordListRow.TextUthmaniSimple)}",
                text_imlaei_simple AS "{nameof(UniqueWordListRow.TextImlaeiSimple)}",
                word_key_imlaei_simple AS "{nameof(UniqueWordListRow.WordKeyImlaeiSimple)}",
                qpc_glyph AS "{nameof(UniqueWordListRow.QpcGlyph)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_surah_number AS "{nameof(UniqueWordListRow.FirstSurahNumber)}",
                first_ayah_number AS "{nameof(UniqueWordListRow.FirstAyahNumber)}",
                first_location AS "{nameof(UniqueWordListRow.FirstLocation)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                word_key_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_simple
            """;

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{EscapeLikePattern(normalizedSearch)}%";
            sql += " WHERE translate(lower(text_uthmani_simple), @foldFrom, @foldTo) ILIKE @pattern"
                + " OR translate(lower(text_imlaei_simple), @foldFrom, @foldTo) ILIKE @pattern"
                + " OR translate(lower(word_key_imlaei_simple), @foldFrom, @foldTo) ILIKE @pattern";
            return _db.Database.SqlQueryRaw<UniqueWordListRow>(
                sql,
                new NpgsqlParameter("foldFrom", FoldFrom),
                new NpgsqlParameter("foldTo", FoldTo),
                new NpgsqlParameter("pattern", pattern));
        }

        return _db.Database.SqlQueryRaw<UniqueWordListRow>(sql);
    }

    private static IQueryable<UniqueWordListRow> ApplySort(IQueryable<UniqueWordListRow> rows, UniqueWordSort sort) => sort switch
    {
        UniqueWordSort.Occurrences => rows
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstWordOrderInMushaf),
        UniqueWordSort.Alpha => rows
            .OrderBy(r => r.SearchText)
            .ThenBy(r => r.FirstWordOrderInMushaf),
        _ => rows.OrderBy(r => r.FirstWordOrderInMushaf),
    };

    private static string? NormalizeArabicQuery(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var builder = new StringBuilder(search.Length);
        foreach (var ch in search)
        {

            if (IsSkippable(ch))
            {
                continue;
            }

            var folded = Fold(ch);
            builder.Append(folded);
        }

        var normalized = builder.ToString().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsSkippable(char ch) =>
        ch == '\u0640' ||
        ch is >= '\u0610' and <= '\u061A' ||
        ch is >= '\u064B' and <= '\u065F' ||
        ch == '\u0670' ||
        ch is >= '\u06D6' and <= '\u06ED' ||
        ch is >= '\u08D3' and <= '\u08FF';

    private static char Fold(char ch)
    {
        var index = FoldFrom.IndexOf(ch);
        return index >= 0 ? FoldTo[index] : ch;
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed record UniqueWordListRow(
        int Id,
        string DisplayTextUthmani,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        string? WordKeyImlaeiSimple,
        string? QpcGlyph,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        short FirstSurahNumber,
        short FirstAyahNumber,
        string FirstLocation,
        int FirstWordOrderInMushaf,
        string SearchText);

    private sealed record UniqueWordHeaderRow(string DisplayTextUthmani, string KindKey);

    private sealed record UniqueWordSummaryRow(
        string DisplayTextUthmani,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        string? WordKeyImlaeiSimple,
        string? QpcGlyph,
        string KindKey,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        short FirstSurahNumber,
        short FirstAyahNumber,
        string FirstLocation);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        string SurahNameArabic);

    private sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);
}
