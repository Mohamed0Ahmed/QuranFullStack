using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    private const string ManualSimilarityAyahsSql = """
        , candidate_occurrences AS MATERIALIZED (
          SELECT occurrence.id,
                 occurrence.variant_id,
                 occurrence.ayah_id,
                 occurrence.start_word_number,
                 occurrence.end_word_number,
                 candidate.matched_count
          FROM candidate_variants AS candidate
          JOIN quran_phrase_occurrences AS occurrence
            ON occurrence.build_id = @build_id
           AND occurrence.variant_id = candidate.variant_id
        ), totals AS (
          SELECT COUNT(DISTINCT ayah_id)::integer AS ayah_count,
                 COUNT(*)::bigint AS occurrence_count
          FROM candidate_occurrences
        ), ranked_ayahs AS (
          SELECT ayah_id,
                 MIN(@word_count - matched_count)::smallint AS difference_count
          FROM candidate_occurrences
          GROUP BY ayah_id
        ), paged_ayahs AS (
          SELECT ranked.ayah_id,
                 ranked.difference_count
          FROM ranked_ayahs AS ranked
          JOIN quran_ayahs AS ayah
            ON ayah.id = ranked.ayah_id
          ORDER BY CASE WHEN @sort_by_strength THEN ranked.difference_count END,
                   ayah.surah_number,
                   ayah.ayah_number
          OFFSET @offset
          LIMIT @page_size
        ), page_occurrences AS (
          SELECT occurrence.id,
                 occurrence.variant_id,
                 occurrence.ayah_id,
                 occurrence.start_word_number,
                 occurrence.end_word_number,
                 variant.exact_token_ids,
                 occurrence.matched_count,
                 ayah.verse_key,
                 ayah.surah_number,
                 surah.name_arabic,
                 ayah.ayah_number,
                 ayah.page_from,
                 ayah.page_to,
                 paged.difference_count
          FROM paged_ayahs AS paged
          JOIN candidate_occurrences AS occurrence
            ON occurrence.ayah_id = paged.ayah_id
          JOIN quran_phrase_variants AS variant
            ON variant.build_id = @build_id
           AND variant.id = occurrence.variant_id
          JOIN quran_ayahs AS ayah
            ON ayah.id = occurrence.ayah_id
          JOIN quran_surahs AS surah
            ON surah.surah_number = ayah.surah_number
        )
        SELECT totals.ayah_count,
               totals.occurrence_count,
               occurrence.id,
               occurrence.variant_id,
               occurrence.ayah_id,
               occurrence.start_word_number,
               occurrence.end_word_number,
               occurrence.exact_token_ids,
               occurrence.matched_count,
               occurrence.verse_key,
               occurrence.surah_number,
               occurrence.name_arabic,
               occurrence.ayah_number,
               occurrence.page_from,
               occurrence.page_to
        FROM totals
        LEFT JOIN page_occurrences AS occurrence
          ON TRUE
        ORDER BY CASE WHEN @sort_by_strength THEN occurrence.difference_count END,
                 occurrence.surah_number,
                 occurrence.ayah_number,
                 occurrence.start_word_number,
                 occurrence.id
        """;

    private const string ManualVariantScoresSql = """
        SELECT variant.id AS variant_id,
               score.matched_count
        FROM quran_phrase_variants AS variant
        CROSS JOIN LATERAL (
          SELECT COUNT(*) FILTER (
                   WHERE variant.exact_token_ids[position] = @exact_token_ids[position]
                 )::smallint AS matched_count
          FROM generate_subscripts(variant.exact_token_ids, 1) AS position
        ) AS score
        WHERE variant.build_id = @build_id
          AND variant.mode = @mode
          AND variant.word_count = @word_count
          AND score.matched_count >= @minimum_matched_words
        ORDER BY variant.id
        """;

    private const string ManualAyahCandidatesSql = """
        WITH candidate_variants AS MATERIALIZED (
          SELECT (@candidate_variant_ids::bigint[])[position] AS variant_id,
                 (@candidate_matched_counts::smallint[])[position] AS matched_count
          FROM generate_series(1, @candidate_count) AS position
        )
        """;

    private async Task<SimilarityAyahSearchPage> ReadManualSimilarityAyahsAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        PhraseSimilaritySort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL jit = off", cancellationToken);
        var candidates = await ReadManualSimilarityCandidatesAsync(
            buildId,
            anchor,
            minimumMatchedWords,
            cancellationToken);
        await using var command = CreateCommand(string.Concat(
            ManualAyahCandidatesSql,
            ManualSimilarityAyahsSql));
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, anchor.WordCount);
        command.Parameters.AddWithValue(
            "candidate_variant_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Bigint,
            candidates.Select(candidate => candidate.VariantId).ToArray());
        command.Parameters.AddWithValue(
            "candidate_matched_counts",
            NpgsqlDbType.Array | NpgsqlDbType.Smallint,
            candidates.Select(candidate => candidate.MatchedCount).ToArray());
        command.Parameters.AddWithValue("candidate_count", candidates.Count);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, CalculateOffset(page, pageSize));
        command.Parameters.AddWithValue("page_size", pageSize);
        command.Parameters.AddWithValue("sort_by_strength", sort == PhraseSimilaritySort.Strength);

        SimilarityAyahTotals? totals = null;
        var rows = new List<SimilarityAyahOccurrenceRow>();
        await using (var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                totals ??= new SimilarityAyahTotals(reader.GetInt32(0), reader.GetInt64(1));
                if (reader.IsDBNull(2))
                {
                    continue;
                }

                rows.Add(new SimilarityAyahOccurrenceRow(
                    reader.GetInt64(2),
                    reader.GetInt64(3),
                    reader.GetInt32(4),
                    reader.GetInt16(5),
                    reader.GetInt16(6),
                    reader.GetFieldValue<int[]>(7),
                    reader.GetInt16(8),
                    reader.GetString(9),
                    reader.GetInt16(10),
                    reader.GetString(11),
                    reader.GetInt16(12),
                    reader.GetInt16(13),
                    reader.GetInt16(14)));
            }
        }

        var resolvedTotals = totals
            ?? throw new InvalidDataException("PhraseSearch manual similarity query returned no totals row.");
        var wordsByAyah = await occurrenceHydrator.LoadAyahWordsAsync(
            rows.Select(row => row.AyahId).Distinct().ToList(),
            cancellationToken);
        var items = rows
            .GroupBy(row => row.AyahId)
            .Select(group => CreateAyah(anchor, group.ToList(), wordsByAyah))
            .ToList();
        return new SimilarityAyahSearchPage(resolvedTotals, items);
    }

    private async Task<IReadOnlyList<ManualSimilarityCandidate>> ReadManualSimilarityCandidatesAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(ManualVariantScoresSql);
        AddManualParameters(command, buildId, anchor, minimumMatchedWords);
        var rows = new List<ManualSimilarityCandidate>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ManualSimilarityCandidate(
                reader.GetInt64(0),
                reader.GetInt16(1)));
        }

        return rows;
    }

    private sealed record SimilarityAyahSearchPage(
        SimilarityAyahTotals Totals,
        IReadOnlyList<PhraseSimilarityAyahDto> Items);

    private sealed record ManualSimilarityCandidate(
        long VariantId,
        short MatchedCount);
}
