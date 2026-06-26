using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

public sealed partial class EfStemsReader
{
    /// <summary>
    /// Loads the complete stem summary list in a bounded aggregation: identity,
    /// nullable dominant lemma/root relationships, derived counts, first verse
    /// key, and the ordered per-stem POS distribution.
    /// </summary>
    internal async Task<IReadOnlyList<StemSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                s.id AS "{nameof(StemAggregationRow.Id)}",
                s.stem_text AS "{nameof(StemAggregationRow.StemText)}",
                replace(translate(lower(s.stem_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(StemAggregationRow.NormalizedStemText)}",
                COALESCE(agg.occurrences_count, 0) AS "{nameof(StemAggregationRow.OccurrencesCount)}",
                COALESCE(agg.ayahs_count, 0) AS "{nameof(StemAggregationRow.AyahsCount)}",
                COALESCE(agg.surahs_count, 0) AS "{nameof(StemAggregationRow.SurahsCount)}",
                COALESCE(agg.simple_words_count, 0) AS "{nameof(StemAggregationRow.SimpleWordsCount)}",
                COALESCE(agg.tashkeel_words_count, 0) AS "{nameof(StemAggregationRow.TashkeelWordsCount)}",
                agg.first_surah_number AS "{nameof(StemAggregationRow.FirstSurahNumber)}",
                agg.first_ayah_number AS "{nameof(StemAggregationRow.FirstAyahNumber)}",
                agg.first_word_number AS "{nameof(StemAggregationRow.FirstWordNumber)}",
                s.first_word_order_in_mushaf AS "{nameof(StemAggregationRow.FirstWordOrderInMushaf)}"
            FROM quran_stems s
            LEFT JOIN (
                SELECT
                    m.stem_id AS sid,
                    COUNT(*) AS occurrences_count,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    (ARRAY_AGG(w.surah_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_surah_number,
                    (ARRAY_AGG(w.ayah_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_ayah_number,
                    (ARRAY_AGG(w.word_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_word_number
                FROM quran_word_morphology m
                JOIN quran_words w ON w.id = m.quran_word_id
                WHERE m.stem_id IS NOT NULL
                GROUP BY m.stem_id
            ) agg ON agg.sid = s.id
            """;

        var aggregates = await _db.Database.SqlQueryRaw<StemAggregationRow>(
            sql,
            new NpgsqlParameter("foldFrom", StemsListDerivation.ArabicFoldFrom),
            new NpgsqlParameter("foldTo", StemsListDerivation.ArabicFoldTo))
            .ToListAsync(cancellationToken);

        if (aggregates.Count == 0)
        {
            return [];
        }

        var occurrenceRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            join t in _db.PosTags.AsNoTracking() on m.HeadPos equals t.Code
            join l in _db.QuranLemmas.AsNoTracking() on m.LemmaId equals l.Id into lemmaJoin
            from l in lemmaJoin.DefaultIfEmpty()
            join r in _db.QuranRoots.AsNoTracking() on m.RootId equals r.Id into rootJoin
            from r in rootJoin.DefaultIfEmpty()
            where m.StemId != null
            select new StemTypeOccurrenceRow(
                m.StemId!.Value,
                m.QuranWordId,
                m.LemmaId,
                l == null ? null : l.LemmaText,
                l == null ? null : l.LemmaBuckwalter,
                m.RootId,
                r == null ? null : r.RootText,
                r == null ? null : r.RootBuckwalter,
                t.Code,
                t.ArabicLabel,
                t.EnglishLabel,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber))
            .ToListAsync(cancellationToken);

        var rowsByStem = occurrenceRows
            .GroupBy(r => r.StemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<StemTypeOccurrenceRow>)g.ToList());

        return aggregates
            .Select(a =>
            {
                var rows = rowsByStem.GetValueOrDefault(a.Id, []);
                var dominantLemma = BuildDominantLemma(rows);
                var dominantRoot = BuildDominantRoot(rows);
                var typeDistribution = MaterializeTypeDistribution(rows);

                return new StemSummaryRow(
                    a.Id,
                    a.StemText,
                    StemsListDerivation.NormalizeArabicQuery(a.StemText) ?? string.Empty,
                    dominantLemma?.Id,
                    dominantLemma?.Text,
                    dominantLemma?.Buckwalter,
                    dominantRoot?.Id,
                    dominantRoot?.Text,
                    dominantRoot?.Buckwalter,
                    a.OccurrencesCount,
                    a.AyahsCount,
                    a.SurahsCount,
                    a.SimpleWordsCount,
                    a.TashkeelWordsCount,
                    BuildFirstVerseKey(a.FirstSurahNumber, a.FirstAyahNumber),
                    a.FirstWordOrderInMushaf,
                    typeDistribution);
            })
            .ToList();
    }
}
