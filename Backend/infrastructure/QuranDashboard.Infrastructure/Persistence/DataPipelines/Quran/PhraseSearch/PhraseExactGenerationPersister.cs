using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using System.Diagnostics;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseExactGenerationPersister
{
    private const string PrepareVariantStagingSql = """
        CREATE INDEX ix_phrase_windows_identity
          ON phrase_windows (mode, word_count, exact_token_ids);
        ANALYZE phrase_windows;

        CREATE TEMP TABLE phrase_variant_stage (
          id bigint NOT NULL,
          mode smallint NOT NULL,
          word_count smallint NOT NULL,
          exact_token_ids integer[] NOT NULL,
          search_token_ids integer[] NOT NULL,
          occurrence_count bigint NOT NULL,
          ayah_count integer NOT NULL,
          surah_count smallint NOT NULL,
          first_quran_word_id integer NOT NULL
        ) ON COMMIT DROP;
        """;
    private const string InsertVariantPartitionSql = """
        INSERT INTO phrase_variant_stage (
          id,
          mode,
          word_count,
          exact_token_ids,
          search_token_ids,
          occurrence_count,
          ayah_count,
          surah_count,
          first_quran_word_id
        )
        SELECT @id_offset + ROW_NUMBER() OVER (ORDER BY grouped.exact_token_ids),
               @mode,
               @word_count,
               grouped.exact_token_ids,
               grouped.search_token_ids,
               grouped.occurrence_count,
               grouped.ayah_count,
               grouped.surah_count,
               grouped.first_quran_word_id
        FROM (
          SELECT exact_token_ids,
                 search_token_ids,
                 COUNT(*)::bigint AS occurrence_count,
                 COUNT(DISTINCT ayah_id)::integer AS ayah_count,
                 COUNT(DISTINCT surah_number)::smallint AS surah_count,
                 MIN(first_quran_word_id)::integer AS first_quran_word_id
          FROM phrase_windows
          WHERE mode = @mode
            AND word_count = @word_count
          GROUP BY exact_token_ids, search_token_ids
        ) AS grouped
        ORDER BY grouped.exact_token_ids
        """;
    private const string FinalizeVariantStagingSql = """
        CREATE UNIQUE INDEX ux_phrase_variant_stage_id
          ON phrase_variant_stage (id);
        CREATE UNIQUE INDEX ux_phrase_variant_stage_identity
          ON phrase_variant_stage (mode, word_count, exact_token_ids);
        ANALYZE phrase_variant_stage;
        """;
    private const string PersistVariantsSql = """
        INSERT INTO quran_phrase_variants (
          build_id,
          id,
          mode,
          word_count,
          exact_token_ids,
          search_token_ids,
          display_text,
          occurrence_count,
          ayah_count,
          surah_count,
          first_quran_word_id
        )
        SELECT @build_id,
               variant.id,
               variant.mode,
               variant.word_count,
               variant.exact_token_ids,
               variant.search_token_ids,
               array_to_string(
                 ayah.display_words[
                   source.word_number:source.word_number + variant.word_count - 1
                 ],
                 ' '
               ),
               variant.occurrence_count,
               variant.ayah_count,
               variant.surah_count,
               variant.first_quran_word_id
        FROM phrase_variant_stage AS variant
        JOIN phrase_source_tokens AS source
          ON source.id = variant.first_quran_word_id
        JOIN phrase_source_ayahs AS ayah
          ON ayah.ayah_id = source.ayah_id
        ORDER BY variant.id
        """;
    private const string PersistOccurrencePartitionSql = """
        INSERT INTO quran_phrase_occurrences (
          build_id,
          id,
          variant_id,
          mode,
          word_count,
          ayah_id,
          start_word_number,
          end_word_number,
          first_quran_word_id,
          last_quran_word_id
        )
        SELECT @build_id,
               @id_offset + ROW_NUMBER() OVER (
                 ORDER BY phrase_window.ayah_id,
                          phrase_window.start_word_number
               ),
               variant.id,
               phrase_window.mode,
               phrase_window.word_count,
               phrase_window.ayah_id,
               phrase_window.start_word_number,
               phrase_window.end_word_number,
               phrase_window.first_quran_word_id,
               phrase_window.last_quran_word_id
        FROM phrase_windows AS phrase_window
        JOIN phrase_variant_stage AS variant
          ON variant.mode = phrase_window.mode
         AND variant.word_count = phrase_window.word_count
         AND variant.exact_token_ids = phrase_window.exact_token_ids
        WHERE phrase_window.mode = @mode
          AND phrase_window.word_count = @word_count
        ORDER BY phrase_window.ayah_id,
                 phrase_window.start_word_number
        """;
    private const string ExactTotalsSql = """
        SELECT (SELECT COUNT(*) FROM quran_phrase_search_tokens WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_variants WHERE build_id = @build_id),
               (SELECT COUNT(*) FROM quran_phrase_occurrences WHERE build_id = @build_id)
        """;

    internal async Task<PhraseExactStageResult> PersistAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        IReadOnlyList<PhraseLengthBuildMetric> windowMetrics,
        CancellationToken ct)
    {
        var metrics = windowMetrics.ToList();
        var operation = "variant-staging-preparation";
        try
        {
            await ExecuteAsync(connection, transaction, PrepareVariantStagingSql, ct);
            long idOffset = 0;
            for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
            {
                var metric = metrics[metricIndex];
                operation = $"variant-staging-{metric.Mode}-{metric.WordCount}";
                var stopwatch = Stopwatch.StartNew();
                var variants = await InsertVariantPartitionAsync(
                    connection,
                    transaction,
                    metric.Mode,
                    metric.WordCount,
                    idOffset,
                    ct);
                stopwatch.Stop();
                idOffset += variants;
                metrics[metricIndex] = metric with
                {
                    Variants = variants,
                    ElapsedMilliseconds = metric.ElapsedMilliseconds + stopwatch.ElapsedMilliseconds,
                };
            }

            operation = "variant-staging-finalization";
            await ExecuteAsync(connection, transaction, FinalizeVariantStagingSql, ct);
            operation = "variant-persistence";
            await ExecuteAsync(
                connection,
                transaction,
                PersistVariantsSql,
                ct,
                new NpgsqlParameter("build_id", buildId));

            long occurrenceOffset = 0;
            for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
            {
                var metric = metrics[metricIndex];
                operation = $"occurrence-persistence-{metric.Mode}-{metric.WordCount}";
                var stopwatch = Stopwatch.StartNew();
                var occurrences = await PersistOccurrencePartitionAsync(
                    connection,
                    transaction,
                    buildId,
                    metric.Mode,
                    metric.WordCount,
                    occurrenceOffset,
                    ct);
                stopwatch.Stop();
                occurrenceOffset += occurrences;
                metrics[metricIndex] = metric with
                {
                    ElapsedMilliseconds = metric.ElapsedMilliseconds + stopwatch.ElapsedMilliseconds,
                };
            }

            operation = "exact-totals";
            var totals = await ReadTotalsAsync(connection, transaction, buildId, ct);
            return new PhraseExactStageResult(totals, metrics);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Phrase exact generation persistence failed during {operation}.",
                exception);
        }
    }

    private static async Task<long> InsertVariantPartitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        short mode,
        short wordCount,
        long idOffset,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(InsertVariantPartitionSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, wordCount);
        command.Parameters.AddWithValue("id_offset", NpgsqlDbType.Bigint, idOffset);
        return await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<long> PersistOccurrencePartitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        short mode,
        short wordCount,
        long idOffset,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(PersistOccurrencePartitionSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", NpgsqlDbType.Uuid, buildId);
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, wordCount);
        command.Parameters.AddWithValue("id_offset", NpgsqlDbType.Bigint, idOffset);
        return await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<PhraseIndexBuildTotals> ReadTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(ExactTotalsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new PhraseIndexBuildTotals(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            0,
            0);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(ct);
    }
}
