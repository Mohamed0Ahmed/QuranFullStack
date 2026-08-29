namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseExactSourcePreparer
{
    internal PhrasePreparedSource Prepare(IReadOnlyList<PhraseSourceToken> sourceTokens)
    {
        var simple = BuildSearchTokens(
            sourceTokens,
            token => token.UniqueSimpleWordId,
            token => token.WordKeyImlaeiSimple,
            mode: 1);
        var tashkil = BuildSearchTokens(
            sourceTokens,
            token => token.UniqueTashkeelWordId,
            token => token.TashkilIdentity,
            mode: 2);

        var tokens = sourceTokens
            .Select(token => new PhrasePreparedSourceToken(
                token.Id,
                token.AyahId,
                token.SurahNumber,
                token.WordNumber,
                token.TextUthmani,
                token.UniqueSimpleWordId,
                simple.ExactToSearchId[token.UniqueSimpleWordId],
                token.UniqueTashkeelWordId,
                tashkil.ExactToSearchId[token.UniqueTashkeelWordId]))
            .ToList();

        return new PhrasePreparedSource(
            tokens,
            simple.SearchTokens.Concat(tashkil.SearchTokens).ToList());
    }

    internal async Task CopySourceTokensAsync(
        NpgsqlConnection connection,
        IReadOnlyList<PhrasePreparedSourceToken> tokens,
        CancellationToken ct)
    {
        const string copySql = """
            COPY phrase_source_tokens (
              id, ayah_id, surah_number, word_number, text_uthmani,
              simple_exact_id, simple_search_id, tashkil_exact_id, tashkil_search_id
            ) FROM STDIN (FORMAT BINARY)
            """;
        await using var importer = await connection.BeginBinaryImportAsync(copySql, ct);
        importer.Timeout = TimeSpan.FromSeconds(PhraseIndexBuildConstants.CommandTimeoutSeconds);
        foreach (var token in tokens)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(token.Id, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(token.AyahId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(token.SurahNumber, NpgsqlDbType.Smallint, ct);
            await importer.WriteAsync(token.WordNumber, NpgsqlDbType.Smallint, ct);
            await importer.WriteAsync(token.TextUthmani, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(token.SimpleExactId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(token.SimpleSearchId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(token.TashkilExactId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(token.TashkilSearchId, NpgsqlDbType.Integer, ct);
        }

        await importer.CompleteAsync(ct);
    }

    internal async Task CopySearchTokensAsync(
        NpgsqlConnection connection,
        Guid buildId,
        IReadOnlyList<PhrasePreparedSearchToken> tokens,
        CancellationToken ct)
    {
        const string copySql = """
            COPY quran_phrase_search_tokens (
              build_id, mode, id, search_text, exact_token_ids
            ) FROM STDIN (FORMAT BINARY)
            """;
        await using var importer = await connection.BeginBinaryImportAsync(copySql, ct);
        importer.Timeout = TimeSpan.FromSeconds(PhraseIndexBuildConstants.CommandTimeoutSeconds);
        foreach (var token in tokens)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(buildId, NpgsqlDbType.Uuid, ct);
            await importer.WriteAsync(token.Mode, NpgsqlDbType.Smallint, ct);
            await importer.WriteAsync((long)token.Id, NpgsqlDbType.Bigint, ct);
            await importer.WriteAsync(token.SearchText, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(token.ExactTokenIds, NpgsqlDbType.Array | NpgsqlDbType.Integer, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static SearchTokenBuildResult BuildSearchTokens(
        IReadOnlyList<PhraseSourceToken> sourceTokens,
        Func<PhraseSourceToken, int> exactIdSelector,
        Func<PhraseSourceToken, string> identitySelector,
        short mode)
    {
        var exactSpellings = sourceTokens
            .GroupBy(exactIdSelector)
            .ToDictionary(
                group => group.Key,
                group => PhraseSearchSpellingNormalizer.Normalize(identitySelector(group.First())));

        if (exactSpellings.Values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("PhraseSearch normalization produced an empty search identity.");
        }

        var groups = exactSpellings
            .GroupBy(pair => pair.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToList();
        var exactToSearchId = new Dictionary<int, int>(exactSpellings.Count);
        var searchTokens = new List<PhrasePreparedSearchToken>(groups.Count);

        for (var index = 0; index < groups.Count; index++)
        {
            var id = index + 1;
            var exactIds = groups[index]
                .Select(pair => pair.Key)
                .Order()
                .ToArray();
            foreach (var exactId in exactIds)
            {
                exactToSearchId.Add(exactId, id);
            }

            searchTokens.Add(new PhrasePreparedSearchToken(mode, id, groups[index].Key, exactIds));
        }

        return new SearchTokenBuildResult(exactToSearchId, searchTokens);
    }

    private sealed record SearchTokenBuildResult(
        IReadOnlyDictionary<int, int> ExactToSearchId,
        IReadOnlyList<PhrasePreparedSearchToken> SearchTokens);
}
