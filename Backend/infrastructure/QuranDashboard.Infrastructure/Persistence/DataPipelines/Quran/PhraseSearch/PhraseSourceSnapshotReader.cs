using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

public sealed class PhraseSourceSnapshotReader
{
    private const string SourceCountsSql = """
        SELECT COUNT(*)::integer,
               COUNT(DISTINCT ayah_id)::integer,
               COALESCE(MAX(words_in_ayah), 0)::smallint
        FROM (
          SELECT ayah_id, COUNT(*) OVER (PARTITION BY ayah_id) AS words_in_ayah
          FROM quran_words
          WHERE is_ayah_marker = false
        ) AS readable
        """;

    private const string SourceTokensSql = QuranTashkeelIdentitySql.IdentityCte + """
        SELECT word.id,
               word.ayah_id,
               word.surah_number,
               word.word_number,
               word.text_uthmani,
               word.word_key_imlaei_simple,
               btrim(translate(word.text_uthmani, identity.ignored_tashkeel_marks, '')) AS tashkil_identity,
               word.unique_simple_word_id,
               word.unique_tashkeel_word_id,
               simple_word.word_key_imlaei_simple = word.word_key_imlaei_simple AS simple_link_consistent,
               btrim(translate(tashkil_word.text_uthmani, identity.ignored_tashkeel_marks, ''))
                 = btrim(translate(word.text_uthmani, identity.ignored_tashkeel_marks, '')) AS tashkil_link_consistent,
               word.is_ayah_marker,
               owning_ayah.surah_number = word.surah_number AS surah_ownership_consistent
        FROM quran_words AS word
        CROSS JOIN display_word_identity AS identity
        LEFT JOIN quran_ayahs AS owning_ayah
          ON owning_ayah.id = word.ayah_id
        LEFT JOIN quran_words_unique_simple AS simple_word
          ON simple_word.id = word.unique_simple_word_id
        LEFT JOIN quran_words_unique_tashkeel AS tashkil_word
          ON tashkil_word.id = word.unique_tashkeel_word_id
        WHERE word.is_ayah_marker = false
        ORDER BY word.id
        """;

    internal async Task<PhraseSourceReadResult> ReadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct,
        int expectedReadableWords = PhraseIndexBuildConstants.ExpectedReadableWords,
        int? expectedAyahs = PhraseIndexBuildConstants.ExpectedAyahs)
    {
        var (readableCount, ayahCount, maximumAyahLength) =
            await ReadCountsAsync(connection, transaction, ct);
        var tokens = new List<PhraseSourceToken>(readableCount);
        var missingLinks = 0;
        var inconsistentLinks = 0;
        var emptyIdentities = 0;
        var markerRows = 0;
        var surahOwnershipViolations = 0;

        await using (var command = new NpgsqlCommand(SourceTokensSql, connection, transaction))
        {
            command.CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds;
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                ct);

            while (await reader.ReadAsync(ct))
            {
                var id = reader.GetInt32(0);
                var ayahId = reader.GetInt32(1);
                var surahNumber = reader.GetInt16(2);
                var wordNumber = reader.GetInt16(3);
                var textUthmani = reader.GetString(4);
                var simpleIdentity = reader.GetString(5);
                var tashkilIdentity = reader.GetString(6);
                var simpleId = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);
                var tashkilId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);
                var simpleConsistent = !reader.IsDBNull(9) && reader.GetBoolean(9);
                var tashkilConsistent = !reader.IsDBNull(10) && reader.GetBoolean(10);
                var isAyahMarker = reader.GetBoolean(11);
                var surahOwnershipConsistent = !reader.IsDBNull(12) && reader.GetBoolean(12);

                if (simpleId <= 0 || tashkilId <= 0)
                {
                    missingLinks++;
                }

                if (!simpleConsistent || !tashkilConsistent)
                {
                    inconsistentLinks++;
                }

                if (string.IsNullOrWhiteSpace(simpleIdentity)
                    || string.IsNullOrWhiteSpace(tashkilIdentity))
                {
                    emptyIdentities++;
                }

                if (isAyahMarker)
                {
                    markerRows++;
                }

                if (!surahOwnershipConsistent)
                {
                    surahOwnershipViolations++;
                }

                tokens.Add(new PhraseSourceToken(
                    id,
                    ayahId,
                    surahNumber,
                    wordNumber,
                    textUthmani,
                    simpleIdentity,
                    tashkilIdentity,
                    simpleId,
                    tashkilId));
            }
        }

        var sequenceViolations = CountSequenceViolations(tokens);
        var checks = new List<PhraseBuildCheck>
        {
            HardCheck(
                "SOURCE-READABLE-WORDS",
                expectedReadableWords,
                readableCount),
            expectedAyahs.HasValue
                ? HardCheck("SOURCE-AYAH-COUNT", expectedAyahs.Value, ayahCount)
                : new PhraseBuildCheck(
                    "SOURCE-AYAH-COUNT",
                    "hard",
                    "> 0",
                    ayahCount.ToString(CultureInfo.InvariantCulture),
                    ayahCount > 0),
            HardCheck("SOURCE-SNAPSHOT-COMPLETE", readableCount, tokens.Count),
            HardCheck("SOURCE-IDENTITY-LINKS", 0, missingLinks),
            HardCheck("SOURCE-IDENTITY-CONSISTENCY", 0, inconsistentLinks),
            HardCheck("SOURCE-IDENTITIES-NONEMPTY", 0, emptyIdentities),
            HardCheck("SOURCE-SNAPSHOT-NO-MARKERS", 0, markerRows),
            HardCheck("SOURCE-SURAH-OWNERSHIP", 0, surahOwnershipViolations),
            HardCheck("SOURCE-AYAH-WORD-CONTIGUITY", 0, sequenceViolations),
        };

        return new PhraseSourceReadResult(tokens, ayahCount, maximumAyahLength, checks);
    }

    private static int CountSequenceViolations(IReadOnlyList<PhraseSourceToken> tokens)
    {
        var violations = 0;
        var currentAyahId = 0;
        short expectedWordNumber = 1;

        foreach (var token in tokens.OrderBy(token => token.AyahId).ThenBy(token => token.WordNumber))
        {
            if (token.AyahId != currentAyahId)
            {
                currentAyahId = token.AyahId;
                expectedWordNumber = 1;
            }

            if (token.WordNumber != expectedWordNumber)
            {
                violations++;
            }

            expectedWordNumber++;
        }

        return violations;
    }

    private static async Task<(int ReadableCount, int AyahCount, short MaximumAyahLength)> ReadCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(SourceCountsSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt16(2));
    }

    private static PhraseBuildCheck HardCheck(string id, int expected, int observed) =>
        new(
            id,
            "hard",
            expected.ToString(CultureInfo.InvariantCulture),
            observed.ToString(CultureInfo.InvariantCulture),
            expected == observed);
}
