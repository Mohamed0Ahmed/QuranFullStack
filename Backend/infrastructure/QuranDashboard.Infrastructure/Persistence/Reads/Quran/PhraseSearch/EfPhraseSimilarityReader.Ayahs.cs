using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    private const string DirectAyahCandidatesSql = """
        WITH candidate_variants AS (
          SELECT @anchor_variant_id::bigint AS variant_id,
                 @word_count::smallint AS matched_count
          UNION ALL
          SELECT right_variant_id,
                 matched_count
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
            AND left_variant_id = @anchor_variant_id
            AND matched_count >= @minimum_matched_words
          UNION ALL
          SELECT left_variant_id,
                 matched_count
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
            AND right_variant_id = @anchor_variant_id
            AND matched_count >= @minimum_matched_words
        )
        """;

    private const string SimilarityAyahTotalsSql = """
        , candidate_occurrences AS (
          SELECT occurrence.ayah_id
          FROM candidate_variants AS candidate
          JOIN quran_phrase_occurrences AS occurrence
            ON occurrence.build_id = @build_id
           AND occurrence.variant_id = candidate.variant_id
        )
        SELECT COUNT(DISTINCT ayah_id)::integer,
               COUNT(*)::bigint
        FROM candidate_occurrences
        """;

    private const string SimilarityAyahPageSql = """
        , candidate_occurrences AS (
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
        )
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
               ayah.page_to
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
        ORDER BY CASE WHEN @sort_by_strength THEN paged.difference_count END,
                 ayah.surah_number,
                 ayah.ayah_number,
                 occurrence.start_word_number,
                 occurrence.id
        """;

    private async Task<SimilarityAyahTotals> ReadSimilarityAyahTotalsAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        CancellationToken cancellationToken)
    {
        await using var command = CreateAyahCommand(
            SimilarityAyahTotalsSql,
            buildId,
            anchor,
            minimumMatchedWords);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidDataException("PhraseSearch similarity totals query returned no row.");
        }

        return new SimilarityAyahTotals(reader.GetInt32(0), reader.GetInt64(1));
    }

    private async Task<IReadOnlyList<PhraseSimilarityAyahDto>> ReadSimilarityAyahPageAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        PhraseSimilaritySort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateAyahCommand(
            SimilarityAyahPageSql,
            buildId,
            anchor,
            minimumMatchedWords);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, CalculateOffset(page, pageSize));
        command.Parameters.AddWithValue("page_size", pageSize);
        command.Parameters.AddWithValue("sort_by_strength", sort == PhraseSimilaritySort.Strength);
        var rows = new List<SimilarityAyahOccurrenceRow>();
        await using (var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new SimilarityAyahOccurrenceRow(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetInt32(2),
                    reader.GetInt16(3),
                    reader.GetInt16(4),
                    reader.GetFieldValue<int[]>(5),
                    reader.GetInt16(6),
                    reader.GetString(7),
                    reader.GetInt16(8),
                    reader.GetString(9),
                    reader.GetInt16(10),
                    reader.GetInt16(11),
                    reader.GetInt16(12)));
            }
        }

        var wordsByAyah = await occurrenceHydrator.LoadAyahWordsAsync(
            rows.Select(row => row.AyahId).Distinct().ToList(),
            cancellationToken);
        return rows
            .GroupBy(row => row.AyahId)
            .Select(group => CreateAyah(anchor, group.ToList(), wordsByAyah))
            .ToList();
    }

    private NpgsqlCommand CreateAyahCommand(
        string tailSql,
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords)
    {
        var command = CreateCommand(string.Concat(
            DirectAyahCandidatesSql,
            tailSql));
        AddNeighborParameters(command, buildId, anchor.Id, minimumMatchedWords);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, anchor.WordCount);

        return command;
    }

    private static PhraseSimilarityAyahDto CreateAyah(
        SimilarityVariantRow anchor,
        IReadOnlyList<SimilarityAyahOccurrenceRow> occurrences,
        IReadOnlyDictionary<int, IReadOnlyList<PhraseAyahWordDto>> wordsByAyah)
    {
        var first = occurrences[0];
        var words = wordsByAyah.GetValueOrDefault(first.AyahId, []);
        var phraseWordIds = new HashSet<int>();
        var matchedWordIds = new HashSet<int>();
        var differingWordIds = new HashSet<int>();
        PhraseHammingScore? bestScore = null;
        foreach (var occurrence in occurrences)
        {
            var phraseWords = words
                .Where(word => word.WordNumber >= occurrence.StartWordNumber
                    && word.WordNumber <= occurrence.EndWordNumber)
                .ToList();
            if (phraseWords.Count != anchor.WordCount)
            {
                throw new InvalidDataException("PhraseSearch similarity occurrence is not a contiguous Quran window.");
            }

            var score = PhraseHammingScore.Calculate(anchor.ExactTokenIds, occurrence.ExactTokenIds);
            if (score.MatchedCount != occurrence.StoredMatchedCount)
            {
                throw new InvalidDataException("PhraseSearch similarity score does not match its exact token arrays.");
            }

            foreach (var word in phraseWords)
            {
                phraseWordIds.Add(word.QuranWordId);
            }
            AddPositionWordIds(matchedWordIds, phraseWords, score.MatchedPositions);
            AddPositionWordIds(differingWordIds, phraseWords, score.DifferingPositions);
            if (bestScore is null || score.DifferenceCount < bestScore.DifferenceCount)
            {
                bestScore = score;
            }
        }

        differingWordIds.ExceptWith(matchedWordIds);
        var best = bestScore ?? throw new InvalidDataException("PhraseSearch similarity ayah has no occurrences.");
        return new PhraseSimilarityAyahDto(
            first.AyahId,
            first.VerseKey,
            first.SurahNumber,
            first.SurahNameArabic,
            first.AyahNumber,
            first.PageFrom,
            first.PageTo,
            best.MatchedCount,
            best.DifferenceCount,
            best.MatchPercent,
            occurrences.Count,
            words,
            new PhraseSimilarityHighlightsDto(
                OrderedWordIds(words, phraseWordIds),
                OrderedWordIds(words, matchedWordIds),
                OrderedWordIds(words, differingWordIds)));
    }

    private static void AddPositionWordIds(
        ISet<int> target,
        IReadOnlyList<PhraseAyahWordDto> phraseWords,
        IReadOnlyList<short> positions)
    {
        foreach (var position in positions)
        {
            target.Add(phraseWords[position - 1].QuranWordId);
        }
    }

    private static IReadOnlyList<int> OrderedWordIds(
        IReadOnlyList<PhraseAyahWordDto> words,
        IReadOnlySet<int> selected) => words
        .Where(word => selected.Contains(word.QuranWordId))
        .Select(word => word.QuranWordId)
        .ToList();

    private sealed record SimilarityAyahTotals(int AyahCount, long OccurrenceCount);

    private sealed record SimilarityAyahOccurrenceRow(
        long OccurrenceId,
        long VariantId,
        int AyahId,
        short StartWordNumber,
        short EndWordNumber,
        int[] ExactTokenIds,
        short StoredMatchedCount,
        string VerseKey,
        short SurahNumber,
        string SurahNameArabic,
        short AyahNumber,
        short PageFrom,
        short PageTo);
}
