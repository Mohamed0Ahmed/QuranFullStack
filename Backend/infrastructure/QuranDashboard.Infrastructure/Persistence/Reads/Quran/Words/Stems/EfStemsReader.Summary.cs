using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

public sealed partial class EfStemsReader
{
    // Shared Mushaf-order tiebreak for the per-group first-occurrence coordinate: earliest matching word
    // by (surah, ayah, word) then quran_word_id. ARRAY_AGG(... ORDER BY ...)[1] pins all three coordinate
    // parts to that same earliest row, so it is the real earliest coordinate, never independent minimums.
    private const string StemFirstOccurrenceOrder = "w.surah_number, w.ayah_number, w.word_number, seg.quran_word_id";

    private const string StemMatchingSegmentPredicate =
        "seg.kind = 'STEM' AND seg.stem_id IS NOT NULL AND NOT w.is_ayah_marker";

    /// <summary>
    /// Loads the complete stem summary list in a bounded aggregation: identity, nullable
    /// dominant lemma/root relationships, derived counts, first verse key, and the ordered
    /// per-stem POS distribution. The distribution and the dominant lemma/root winner inputs are grouped
    /// in the database (summary grain), so the read never transfers occurrence-grain rows (B2).
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
                    seg.stem_id AS sid,
                    COUNT(*) AS occurrences_count,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    (ARRAY_AGG(w.surah_number ORDER BY w.surah_number, w.ayah_number, w.word_number, seg.segment_number, seg.id))[1] AS first_surah_number,
                    (ARRAY_AGG(w.ayah_number ORDER BY w.surah_number, w.ayah_number, w.word_number, seg.segment_number, seg.id))[1] AS first_ayah_number,
                    (ARRAY_AGG(w.word_number ORDER BY w.surah_number, w.ayah_number, w.word_number, seg.segment_number, seg.id))[1] AS first_word_number
                FROM quran_word_morphology_segments seg
                JOIN quran_words w ON w.id = seg.quran_word_id
                WHERE seg.kind = 'STEM'
                  AND seg.stem_id IS NOT NULL
                  AND NOT w.is_ayah_marker
                GROUP BY seg.stem_id
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

        // Three compact grouped commands over the same STEM-segment base: one per dimension (POS,
        // dominant-lemma inputs, dominant-root inputs). Each returns one row per (stem, dimension) group,
        // never one row per occurrence.
        var distributionRows = await _db.Database
            .SqlQueryRaw<StemTypeDistributionSqlRow>(StemTypeDistributionSql)
            .ToListAsync(cancellationToken);
        var lemmaGroups = await _db.Database
            .SqlQueryRaw<StemRelationGroupSqlRow>(StemRelationGroupSql("lemma_id", "quran_lemmas", "lemma_text", "lemma_buckwalter"))
            .ToListAsync(cancellationToken);
        var rootGroups = await _db.Database
            .SqlQueryRaw<StemRelationGroupSqlRow>(StemRelationGroupSql("root_id", "quran_roots", "root_text", "root_buckwalter"))
            .ToListAsync(cancellationToken);

        var distributionByStem = distributionRows
            .GroupBy(r => r.StemId)
            .ToDictionary(g => g.Key, g => OrderTypeDistribution(g));
        var dominantLemmaByStem = lemmaGroups
            .GroupBy(r => r.StemId)
            .ToDictionary(g => g.Key, g => PickDominantRelation(g));
        var dominantRootByStem = rootGroups
            .GroupBy(r => r.StemId)
            .ToDictionary(g => g.Key, g => PickDominantRelation(g));

        return aggregates
            .Select(a =>
            {
                var dominantLemma = dominantLemmaByStem.GetValueOrDefault(a.Id);
                var dominantRoot = dominantRootByStem.GetValueOrDefault(a.Id);
                var typeDistribution = distributionByStem.GetValueOrDefault(a.Id, []);

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

    // Per-(stem, POS) distribution: occurrence count and the earliest-occurrence coordinate per group.
    private static string StemTypeDistributionSql => $"""
        SELECT
            seg.stem_id AS "{nameof(StemTypeDistributionSqlRow.StemId)}",
            seg.pos AS "{nameof(StemTypeDistributionSqlRow.Code)}",
            t.arabic_label AS "{nameof(StemTypeDistributionSqlRow.ArabicLabel)}",
            t.english_label AS "{nameof(StemTypeDistributionSqlRow.EnglishLabel)}",
            COUNT(*)::int AS "{nameof(StemTypeDistributionSqlRow.OccurrencesCount)}",
            ((ARRAY_AGG(w.surah_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemTypeDistributionSqlRow.FirstSurahNumber)}",
            ((ARRAY_AGG(w.ayah_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemTypeDistributionSqlRow.FirstAyahNumber)}",
            ((ARRAY_AGG(w.word_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemTypeDistributionSqlRow.FirstWordNumber)}"
        FROM quran_word_morphology_segments seg
        JOIN quran_words w ON w.id = seg.quran_word_id
        JOIN quran_pos_tags t ON t.code = seg.pos
        WHERE {StemMatchingSegmentPredicate}
        GROUP BY seg.stem_id, seg.pos, t.arabic_label, t.english_label
        """;

    // Per-(stem, relation) winner inputs (relation = lemma or root): occurrence count, relation text/
    // buckwalter, and the earliest-occurrence coordinate per group. The relation column/table names are
    // fixed constants (never user input). LEFT JOIN + coalesce-in-C# mirrors the previous behavior for a
    // relation id whose row is absent.
    private static string StemRelationGroupSql(string idColumn, string relationTable, string textColumn, string buckwalterColumn) => $"""
        SELECT
            seg.stem_id AS "{nameof(StemRelationGroupSqlRow.StemId)}",
            seg.{idColumn} AS "{nameof(StemRelationGroupSqlRow.RelationId)}",
            rel.{textColumn} AS "{nameof(StemRelationGroupSqlRow.Text)}",
            rel.{buckwalterColumn} AS "{nameof(StemRelationGroupSqlRow.Buckwalter)}",
            COUNT(*)::int AS "{nameof(StemRelationGroupSqlRow.OccurrencesCount)}",
            ((ARRAY_AGG(w.surah_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemRelationGroupSqlRow.FirstSurahNumber)}",
            ((ARRAY_AGG(w.ayah_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemRelationGroupSqlRow.FirstAyahNumber)}",
            ((ARRAY_AGG(w.word_number ORDER BY {StemFirstOccurrenceOrder}))[1])::int AS "{nameof(StemRelationGroupSqlRow.FirstWordNumber)}"
        FROM quran_word_morphology_segments seg
        JOIN quran_words w ON w.id = seg.quran_word_id
        LEFT JOIN {relationTable} rel ON rel.id = seg.{idColumn}
        WHERE {StemMatchingSegmentPredicate} AND seg.{idColumn} IS NOT NULL
        GROUP BY seg.stem_id, seg.{idColumn}, rel.{textColumn}, rel.{buckwalterColumn}
        """;

    private sealed record StemTypeDistributionSqlRow(
        int StemId,
        string Code,
        string ArabicLabel,
        string EnglishLabel,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);

    private sealed record StemRelationGroupSqlRow(
        int StemId,
        int RelationId,
        string? Text,
        string? Buckwalter,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);
}
