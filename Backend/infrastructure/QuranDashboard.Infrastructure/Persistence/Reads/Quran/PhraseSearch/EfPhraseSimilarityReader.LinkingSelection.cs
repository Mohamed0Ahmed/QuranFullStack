using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    private const string SimilarityLinkingAyahPopulationSql = """
        , candidate_occurrences AS (
          SELECT occurrence.ayah_id
          FROM candidate_variants AS candidate
          JOIN quran_phrase_occurrences AS occurrence
            ON occurrence.build_id = @build_id
           AND occurrence.variant_id = candidate.variant_id
        )
        SELECT occurrence.ayah_id
        FROM candidate_occurrences AS occurrence
        JOIN quran_ayahs AS ayah
          ON ayah.id = occurrence.ayah_id
        GROUP BY occurrence.ayah_id,
                 ayah.surah_number,
                 ayah.ayah_number
        ORDER BY ayah.surah_number,
                 ayah.ayah_number
        """;

    private const string SimilarityLinkingOccurrencesSql = """
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
          WHERE occurrence.ayah_id = ANY(@selected_ayah_ids)
        )
        SELECT occurrence.ayah_id,
               occurrence.start_word_number,
               occurrence.end_word_number,
               variant.exact_token_ids,
               occurrence.matched_count,
               ayah.verse_key,
               ayah.page_from
        FROM candidate_occurrences AS occurrence
        JOIN quran_phrase_variants AS variant
          ON variant.build_id = @build_id
         AND variant.id = occurrence.variant_id
        JOIN quran_ayahs AS ayah
          ON ayah.id = occurrence.ayah_id
        ORDER BY ayah.surah_number,
                 ayah.ayah_number,
                 occurrence.start_word_number,
                 occurrence.id
        """;

    public async Task<PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>> GetLinkingSelectionAsync(
        PhraseResolutionReference resolution,
        short minimumMatchedWords,
        PhraseSimilarityLinkingSelection selection,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(selection.Mode)
            || selection.AyahIds.Any(ayahId => ayahId <= 0)
            || selection.AyahIds.Distinct().Count() != selection.AyahIds.Count)
        {
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidSelection();
        }

        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.BuildChanged();
        }

        var anchor = await LoadVariantAsync(snapshot.ActiveBuildId, resolution, cancellationToken);
        if (anchor is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidReference();
        }

        var manualCandidates = await LoadManualLinkingCandidatesAsync(
            snapshot.ActiveBuildId,
            anchor,
            minimumMatchedWords,
            cancellationToken);
        var populationAyahIds = await ReadLinkingPopulationAyahIdsAsync(
            snapshot.ActiveBuildId,
            anchor,
            minimumMatchedWords,
            manualCandidates,
            cancellationToken);
        var populationAyahIdSet = populationAyahIds.ToHashSet();
        var submittedAyahIds = selection.AyahIds.ToHashSet();
        if (submittedAyahIds.Any(ayahId => !populationAyahIdSet.Contains(ayahId)))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidSelection();
        }

        var selectedAyahIds = selection.Mode switch
        {
            PhraseSimilarityAyahSelectionMode.Only => populationAyahIds
                .Where(submittedAyahIds.Contains)
                .ToList(),
            PhraseSimilarityAyahSelectionMode.AllExcept => populationAyahIds
                .Where(ayahId => !submittedAyahIds.Contains(ayahId))
                .ToList(),
            _ => [],
        };
        if (selectedAyahIds.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidSelection();
        }

        var occurrenceRows = await ReadLinkingOccurrencesAsync(
            snapshot.ActiveBuildId,
            anchor,
            minimumMatchedWords,
            manualCandidates,
            selectedAyahIds,
            cancellationToken);
        var rowsByAyah = occurrenceRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        if (selectedAyahIds.Any(ayahId => !rowsByAyah.ContainsKey(ayahId)))
        {
            throw new InvalidDataException("PhraseSearch similarity selection is missing a qualifying occurrence.");
        }

        var wordsByAyah = await occurrenceHydrator.LoadAyahWordsAsync(
            selectedAyahIds,
            cancellationToken);
        var ayahs = selectedAyahIds
            .Select(ayahId => CreateSimilarityLinkingAyah(
                anchor,
                minimumMatchedWords,
                rowsByAyah[ayahId],
                wordsByAyah.GetValueOrDefault(ayahId, [])))
            .ToList();
        var response = new PhraseSimilarityLinkingSelectionResponse(
            snapshot.ActiveBuildId,
            new PhraseSimilarityLinkingSelectionQueryDto(
                anchor.Id,
                anchor.DisplayText,
                anchor.WordCount),
            ayahs.Count,
            ayahs);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.Success(response);
    }

    private async Task<IReadOnlyList<ManualSimilarityCandidate>?> LoadManualLinkingCandidatesAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        CancellationToken cancellationToken)
    {
        if (anchor.WordCount >= PhraseSimilarityContract.MinimumGlobalLength)
        {
            return null;
        }

        await db.Database.ExecuteSqlRawAsync("SET LOCAL jit = off", cancellationToken);
        return await ReadManualSimilarityCandidatesAsync(
            buildId,
            anchor,
            minimumMatchedWords,
            cancellationToken);
    }

    private async Task<IReadOnlyList<int>> ReadLinkingPopulationAyahIdsAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        IReadOnlyList<ManualSimilarityCandidate>? manualCandidates,
        CancellationToken cancellationToken)
    {
        await using var command = CreateLinkingSelectionCommand(
            SimilarityLinkingAyahPopulationSql,
            buildId,
            anchor,
            minimumMatchedWords,
            manualCandidates);
        var ayahIds = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ayahIds.Add(reader.GetInt32(0));
        }

        return ayahIds;
    }

    private async Task<IReadOnlyList<SimilarityLinkingOccurrenceRow>> ReadLinkingOccurrencesAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        IReadOnlyList<ManualSimilarityCandidate>? manualCandidates,
        IReadOnlyList<int> selectedAyahIds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateLinkingSelectionCommand(
            SimilarityLinkingOccurrencesSql,
            buildId,
            anchor,
            minimumMatchedWords,
            manualCandidates);
        command.Parameters.AddWithValue(
            "selected_ayah_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Integer,
            selectedAyahIds.ToArray());
        var rows = new List<SimilarityLinkingOccurrenceRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SimilarityLinkingOccurrenceRow(
                reader.GetInt32(0),
                reader.GetInt16(1),
                reader.GetInt16(2),
                reader.GetFieldValue<int[]>(3),
                reader.GetInt16(4),
                reader.GetString(5),
                reader.GetInt16(6)));
        }

        return rows;
    }

    private NpgsqlCommand CreateLinkingSelectionCommand(
        string tailSql,
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        IReadOnlyList<ManualSimilarityCandidate>? manualCandidates)
    {
        if (manualCandidates is null)
        {
            return CreateAyahCommand(tailSql, buildId, anchor, minimumMatchedWords);
        }

        var command = CreateCommand(string.Concat(ManualAyahCandidatesSql, tailSql));
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue(
            "candidate_variant_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Bigint,
            manualCandidates.Select(candidate => candidate.VariantId).ToArray());
        command.Parameters.AddWithValue(
            "candidate_matched_counts",
            NpgsqlDbType.Array | NpgsqlDbType.Smallint,
            manualCandidates.Select(candidate => candidate.MatchedCount).ToArray());
        command.Parameters.AddWithValue("candidate_count", manualCandidates.Count);
        return command;
    }

    private static PhraseSimilarityLinkingSelectionAyahDto CreateSimilarityLinkingAyah(
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        IReadOnlyList<SimilarityLinkingOccurrenceRow> occurrences,
        IReadOnlyList<PhraseAyahWordDto> words)
    {
        var first = occurrences[0];
        var selectedWordIds = new HashSet<int>();
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
            if (score.MatchedCount != occurrence.StoredMatchedCount
                || score.MatchedCount < minimumMatchedWords)
            {
                throw new InvalidDataException("PhraseSearch similarity selection contains an invalid score.");
            }

            foreach (var word in phraseWords)
            {
                selectedWordIds.Add(word.QuranWordId);
            }
        }

        var canonicalWordIds = words
            .Where(word => selectedWordIds.Contains(word.QuranWordId))
            .Select(word => word.QuranWordId)
            .ToList();
        if (canonicalWordIds.Count == 0
            || canonicalWordIds.Count != selectedWordIds.Count
            || canonicalWordIds.Any(wordId => wordId <= 0)
            || canonicalWordIds.Distinct().Count() != canonicalWordIds.Count)
        {
            throw new InvalidDataException("PhraseSearch linking selection contains an invalid Quran word.");
        }

        return new PhraseSimilarityLinkingSelectionAyahDto(
            first.AyahId,
            first.VerseKey,
            first.PageNumber,
            canonicalWordIds);
    }

    private sealed record SimilarityLinkingOccurrenceRow(
        int AyahId,
        short StartWordNumber,
        short EndWordNumber,
        int[] ExactTokenIds,
        short StoredMatchedCount,
        string VerseKey,
        short PageNumber);
}
