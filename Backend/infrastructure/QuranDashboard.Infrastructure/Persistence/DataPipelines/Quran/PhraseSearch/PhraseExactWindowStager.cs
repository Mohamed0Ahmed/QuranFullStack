using System.Diagnostics;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseExactWindowStager
{
    private const string CreateTemporaryTablesSql = """
        CREATE TEMP TABLE phrase_source_tokens (
          id integer PRIMARY KEY,
          ayah_id integer NOT NULL,
          surah_number smallint NOT NULL,
          word_number smallint NOT NULL,
          text_uthmani text NOT NULL,
          simple_exact_id integer NOT NULL,
          simple_search_id integer NOT NULL,
          tashkil_exact_id integer NOT NULL,
          tashkil_search_id integer NOT NULL
        ) ON COMMIT DROP;

        CREATE TEMP TABLE phrase_source_ayahs ON COMMIT DROP AS
        SELECT ayah_id,
               MIN(surah_number)::smallint AS surah_number,
               array_agg(id ORDER BY word_number) AS quran_word_ids,
               array_agg(text_uthmani ORDER BY word_number) AS display_words,
               array_agg(simple_exact_id ORDER BY word_number) AS simple_exact_ids,
               array_agg(simple_search_id ORDER BY word_number) AS simple_search_ids,
               array_agg(tashkil_exact_id ORDER BY word_number) AS tashkil_exact_ids,
               array_agg(tashkil_search_id ORDER BY word_number) AS tashkil_search_ids
        FROM phrase_source_tokens
        GROUP BY ayah_id;

        CREATE TEMP TABLE phrase_windows (
          mode smallint NOT NULL,
          word_count smallint NOT NULL,
          exact_token_ids integer[] NOT NULL,
          search_token_ids integer[] NOT NULL,
          ayah_id integer NOT NULL,
          surah_number smallint NOT NULL,
          start_word_number smallint NOT NULL,
          end_word_number smallint NOT NULL,
          first_quran_word_id integer NOT NULL,
          last_quran_word_id integer NOT NULL
        ) ON COMMIT DROP;
        """;

    private const string InsertWindowsSql = """
        INSERT INTO phrase_windows (
          mode,
          word_count,
          exact_token_ids,
          search_token_ids,
          ayah_id,
          surah_number,
          start_word_number,
          end_word_number,
          first_quran_word_id,
          last_quran_word_id
        )
        SELECT @mode,
               @word_count,
               (CASE WHEN @mode = 1 THEN ayah.simple_exact_ids ELSE ayah.tashkil_exact_ids END)
                 [start_index:start_index + @word_count - 1],
               (CASE WHEN @mode = 1 THEN ayah.simple_search_ids ELSE ayah.tashkil_search_ids END)
                 [start_index:start_index + @word_count - 1],
               ayah.ayah_id,
               ayah.surah_number,
               start_index::smallint,
               (start_index + @word_count - 1)::smallint,
               ayah.quran_word_ids[start_index],
               ayah.quran_word_ids[start_index + @word_count - 1]
        FROM phrase_source_ayahs AS ayah
        CROSS JOIN LATERAL generate_series(
          1,
          cardinality(ayah.quran_word_ids) - @word_count + 1
        ) AS start_index
        WHERE cardinality(ayah.quran_word_ids) >= @word_count
        """;

    private readonly PhraseExactSourcePreparer sourcePreparer;

    public PhraseExactWindowStager(PhraseExactSourcePreparer sourcePreparer)
    {
        this.sourcePreparer = sourcePreparer;
    }

    internal async Task<IReadOnlyList<PhraseLengthBuildMetric>> StageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        IReadOnlyList<PhraseSourceToken> sourceTokens,
        short maximumAyahLength,
        CancellationToken ct)
    {
        var operation = "source-preparation";
        try
        {
            var prepared = sourcePreparer.Prepare(sourceTokens);
            operation = "temporary-table-creation";
            await ExecuteAsync(connection, transaction, CreateTemporaryTablesSql, ct);
            operation = "source-copy";
            await sourcePreparer.CopySourceTokensAsync(connection, prepared.Tokens, ct);
            operation = "ayah-snapshot";
            await RebuildAyahSnapshotAsync(connection, transaction, ct);
            operation = "search-token-copy";
            await sourcePreparer.CopySearchTokensAsync(connection, buildId, prepared.SearchTokens, ct);

            var metrics = new List<PhraseLengthBuildMetric>(maximumAyahLength * 2);
            for (short mode = 1; mode <= 2; mode++)
            {
                for (short wordCount = 1; wordCount <= maximumAyahLength; wordCount++)
                {
                    operation = $"window-generation-{mode}-{wordCount}";
                    var stopwatch = Stopwatch.StartNew();
                    var rawWindows = await InsertWindowsAsync(
                        connection,
                        transaction,
                        mode,
                        wordCount,
                        ct);
                    stopwatch.Stop();
                    metrics.Add(new PhraseLengthBuildMetric(
                        mode,
                        wordCount,
                        rawWindows,
                        0,
                        "set-based-window-slice",
                        0,
                        0,
                        0,
                        0,
                        stopwatch.ElapsedMilliseconds,
                        GC.GetTotalMemory(forceFullCollection: false)));
                }
            }

            return metrics;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Phrase exact window staging failed during {operation}.",
                exception);
        }
    }

    private static Task RebuildAyahSnapshotAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            TRUNCATE phrase_source_ayahs;
            INSERT INTO phrase_source_ayahs
            SELECT ayah_id,
                   MIN(surah_number)::smallint,
                   array_agg(id ORDER BY word_number),
                   array_agg(text_uthmani ORDER BY word_number),
                   array_agg(simple_exact_id ORDER BY word_number),
                   array_agg(simple_search_id ORDER BY word_number),
                   array_agg(tashkil_exact_id ORDER BY word_number),
                   array_agg(tashkil_search_id ORDER BY word_number)
            FROM phrase_source_tokens
            GROUP BY ayah_id;
            CREATE UNIQUE INDEX ux_phrase_source_ayahs_id ON phrase_source_ayahs (ayah_id);
            ANALYZE phrase_source_ayahs;
            """,
            ct);

    private static async Task<long> InsertWindowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        short mode,
        short wordCount,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(InsertWindowsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, wordCount);
        return await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await command.ExecuteNonQueryAsync(ct);
    }
}
