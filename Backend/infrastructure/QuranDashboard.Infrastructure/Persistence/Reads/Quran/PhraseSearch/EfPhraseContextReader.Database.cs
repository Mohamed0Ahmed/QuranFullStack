using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private const string FilteredOccurrencesSql = """
        WITH base_occurrences AS MATERIALIZED (
          SELECT occurrence.id AS occurrence_id,
                 occurrence.ayah_id,
                 ayah.verse_key,
                 ayah.surah_number,
                 surah.name_arabic AS surah_name_arabic,
                 ayah.ayah_number,
                 ayah.page_from,
                 ayah.page_to,
                 occurrence.start_word_number,
                 occurrence.end_word_number
          FROM quran_phrase_occurrences AS occurrence
          JOIN quran_ayahs AS ayah
            ON ayah.id = occurrence.ayah_id
          JOIN quran_surahs AS surah
            ON surah.surah_number = ayah.surah_number
          WHERE occurrence.build_id = @build_id
            AND occurrence.variant_id = @variant_id
        ), candidate_ayahs AS MATERIALIZED (
          SELECT DISTINCT ayah_id
          FROM base_occurrences
        ), ayah_word_stats AS MATERIALIZED (
          SELECT candidate.ayah_id,
                 COUNT(word.id)::integer AS readable_word_count,
                 CASE
                   WHEN COUNT(word.id) = 0 THEN TRUE
                   ELSE BOOL_OR(
                     (CASE
                        WHEN @simple_mode THEN word.unique_simple_word_id
                        ELSE word.unique_tashkeel_word_id
                      END) IS NULL)
                 END AS has_invalid_exact_identity
          FROM candidate_ayahs AS candidate
          LEFT JOIN quran_words AS word
            ON word.ayah_id = candidate.ayah_id
           AND NOT word.is_ayah_marker
          GROUP BY candidate.ayah_id
        ), population_integrity AS (
          SELECT COALESCE(BOOL_OR(has_invalid_exact_identity), FALSE) AS has_invalid_exact_identity
          FROM ayah_word_stats
        ), filtered_occurrences AS MATERIALIZED (
          SELECT occurrence.*,
                 stats.readable_word_count
          FROM base_occurrences AS occurrence
          JOIN ayah_word_stats AS stats
            ON stats.ayah_id = occurrence.ayah_id
          WHERE (
              NOT @has_previous_path
              OR NOT EXISTS (
                SELECT 1
                FROM UNNEST(@previous_exact_token_ids::integer[]) WITH ORDINALITY
                  AS selected(exact_token_id, position)
                WHERE NOT EXISTS (
                  SELECT 1
                  FROM quran_words AS path_word
                  WHERE path_word.ayah_id = occurrence.ayah_id
                    AND NOT path_word.is_ayah_marker
                    AND path_word.word_number = occurrence.start_word_number - selected.position::smallint
                    AND (CASE
                           WHEN @simple_mode THEN path_word.unique_simple_word_id
                           ELSE path_word.unique_tashkeel_word_id
                         END) = selected.exact_token_id
                )
              )
            )
            AND (
              NOT @previous_ends_at_boundary
              OR occurrence.start_word_number - 1 = CARDINALITY(@previous_exact_token_ids::integer[])
            )
            AND (
              NOT @has_following_path
              OR NOT EXISTS (
                SELECT 1
                FROM UNNEST(@following_exact_token_ids::integer[]) WITH ORDINALITY
                  AS selected(exact_token_id, position)
                WHERE NOT EXISTS (
                  SELECT 1
                  FROM quran_words AS path_word
                  WHERE path_word.ayah_id = occurrence.ayah_id
                    AND NOT path_word.is_ayah_marker
                    AND path_word.word_number = occurrence.end_word_number + selected.position::smallint
                    AND (CASE
                           WHEN @simple_mode THEN path_word.unique_simple_word_id
                           ELSE path_word.unique_tashkeel_word_id
                         END) = selected.exact_token_id
                )
              )
            )
            AND (
              NOT @following_ends_at_boundary
              OR stats.readable_word_count - occurrence.end_word_number
                = CARDINALITY(@following_exact_token_ids::integer[])
            )
        )
        """;

    private const string OccurrencePageSql = """
        , ranked_occurrences AS (
          SELECT occurrence.*,
                 ROW_NUMBER() OVER (
                   ORDER BY occurrence.surah_number,
                            occurrence.ayah_number,
                            occurrence.start_word_number,
                            occurrence.occurrence_id
                 ) AS row_number
          FROM filtered_occurrences AS occurrence
        ), filtered_summary AS (
          SELECT COUNT(*)::integer AS total_count
          FROM filtered_occurrences
        ), requested_occurrences AS (
          SELECT occurrence.*
          FROM ranked_occurrences AS occurrence
          WHERE occurrence.row_number = 1
             OR (
               occurrence.row_number > @offset
               AND occurrence.row_number <= @offset + @page_size
             )
        )
        SELECT summary.total_count,
               integrity.has_invalid_exact_identity,
               occurrence.row_number,
               occurrence.occurrence_id,
               occurrence.ayah_id,
               occurrence.verse_key,
               occurrence.surah_number,
               occurrence.surah_name_arabic,
               occurrence.ayah_number,
               occurrence.page_from,
               occurrence.page_to,
               occurrence.start_word_number,
               occurrence.end_word_number
        FROM filtered_summary AS summary
        CROSS JOIN population_integrity AS integrity
        LEFT JOIN requested_occurrences AS occurrence
          ON TRUE
        ORDER BY occurrence.row_number
        """;

    private async Task<long?> LoadVariantIdAsync(
        Guid buildId,
        PhraseResolutionReference resolution,
        CancellationToken cancellationToken)
    {
        var exactTokenIds = resolution.ExactTokenIds.ToArray();
        return await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(candidate => candidate.BuildId == buildId
                && candidate.Mode == resolution.Mode
                && candidate.WordCount == exactTokenIds.Length
                && candidate.ExactTokenIds.SequenceEqual(exactTokenIds))
            .Select(candidate => (long?)candidate.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ContextOccurrencePageLoad> ReadOccurrencePageAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        long offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            OccurrencePageSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, offset);
        command.Parameters.AddWithValue("page_size", pageSize);

        var totalCount = 0;
        var hasInvalidExactIdentity = false;
        ContextOccurrenceRow? representative = null;
        var items = new List<ContextOccurrenceRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt32(0);
            hasInvalidExactIdentity = reader.GetBoolean(1);
            if (reader.IsDBNull(2))
            {
                continue;
            }

            var rowNumber = reader.GetInt64(2);
            var occurrence = ReadOccurrenceRow(reader, 3);
            if (rowNumber == 1)
            {
                representative = occurrence;
            }

            if (rowNumber > offset && rowNumber <= offset + pageSize)
            {
                items.Add(occurrence);
            }
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        if (totalCount > 0 && representative is null)
        {
            throw new InvalidDataException("PhraseSearch context query did not return its representative occurrence.");
        }

        return new ContextOccurrencePageLoad(totalCount, representative, items);
    }

    private async Task<IReadOnlyDictionary<long, ContextOccurrence>> LoadContextOccurrencesAsync(
        IReadOnlyList<ContextOccurrenceRow> rows,
        PhraseResolutionReference resolution,
        CancellationToken cancellationToken)
    {
        var uniqueRows = rows
            .DistinctBy(row => row.OccurrenceId)
            .ToList();
        if (uniqueRows.Count == 0)
        {
            return new Dictionary<long, ContextOccurrence>();
        }

        var ayahIds = uniqueRows
            .Select(row => row.AyahId)
            .Distinct()
            .ToList();
        var wordRows = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new ContextWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani,
                resolution.Mode == PhraseTextMode.Simple
                    ? word.UniqueSimpleWordId
                    : word.UniqueTashkeelWordId))
            .ToListAsync(cancellationToken);
        if (wordRows.Any(word => word.ExactTokenId is null))
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        var wordsByAyah = wordRows
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ContextWord>)group
                    .Select(word => new ContextWord(
                        word.QuranWordId,
                        word.WordNumber,
                        word.PageNumber,
                        word.TextUthmani,
                        word.ExactTokenId!.Value))
                    .ToList());
        var occurrences = new Dictionary<long, ContextOccurrence>(uniqueRows.Count);
        foreach (var row in uniqueRows)
        {
            var words = wordsByAyah.GetValueOrDefault(row.AyahId)
                ?? throw new InvalidDataException("PhraseSearch context occurrence has no source words.");
            if (words.Count == 0
                || words.Where((word, index) => word.WordNumber != index + 1).Any()
                || row.StartWordNumber <= 0
                || row.EndWordNumber > words.Count)
            {
                throw new InvalidDataException("PhraseSearch context occurrence does not map to a contiguous Quran ayah.");
            }

            var occurrence = new ContextOccurrence(row, words);
            var exactTokenIds = occurrence.Words
                .Skip(row.StartWordNumber - 1)
                .Take(row.EndWordNumber - row.StartWordNumber + 1)
                .Select(word => word.ExactTokenId);
            if (!exactTokenIds.SequenceEqual(resolution.ExactTokenIds))
            {
                throw new InvalidDataException("PhraseSearch context occurrence does not match its resolved exact identity.");
            }

            occurrences.Add(row.OccurrenceId, occurrence);
        }

        return occurrences;
    }

    private NpgsqlCommand CreateContextCommand(string sql)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("PhraseSearch context read requires an active snapshot transaction.");
        return new NpgsqlCommand(sql, connection, transaction);
    }

    private static void AddContextParameters(
        NpgsqlCommand command,
        Guid buildId,
        long variantId,
        PhraseContextSelection selection)
    {
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("variant_id", variantId);
        command.Parameters.AddWithValue(
            "simple_mode",
            selection.Resolution.Mode == PhraseTextMode.Simple);
        AddPathParameters(command, "previous", selection.Previous);
        AddPathParameters(command, "following", selection.Following);
    }

    private static void AddPathParameters(
        NpgsqlCommand command,
        string prefix,
        PhrasePathReference? path)
    {
        command.Parameters.AddWithValue($"has_{prefix}_path", path is not null);
        command.Parameters.AddWithValue(
            $"{prefix}_exact_token_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Integer,
            path?.SelectedExactTokenIds.ToArray() ?? []);
        command.Parameters.AddWithValue(
            $"{prefix}_ends_at_boundary",
            path?.EndsAtBoundary == true);
    }

    private static ContextOccurrenceRow ReadOccurrenceRow(NpgsqlDataReader reader, int offset) => new(
        reader.GetInt64(offset),
        reader.GetInt32(offset + 1),
        reader.GetString(offset + 2),
        reader.GetInt16(offset + 3),
        reader.GetString(offset + 4),
        reader.GetInt16(offset + 5),
        reader.GetInt16(offset + 6),
        reader.GetInt16(offset + 7),
        reader.GetInt16(offset + 8),
        reader.GetInt16(offset + 9));

    private sealed record ContextOccurrencePageLoad(
        int TotalCount,
        ContextOccurrenceRow? Representative,
        IReadOnlyList<ContextOccurrenceRow> Items);

    private sealed record ContextOccurrence(ContextOccurrenceRow Row, IReadOnlyList<ContextWord> Words);

    private sealed record ContextOccurrenceRow(
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

    private sealed record ContextWord(
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        int ExactTokenId);

    private sealed record ContextWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        int? ExactTokenId);
}
