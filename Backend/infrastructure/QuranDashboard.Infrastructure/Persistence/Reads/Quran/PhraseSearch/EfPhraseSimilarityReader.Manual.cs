using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    private const string ManualScanCountSql = """
        SELECT COUNT(*)::integer
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
          AND variant.id <> @anchor_variant_id
          AND score.matched_count >= @minimum_matched_words
        """;

    private const string ManualScanPageSql = """
        SELECT variant.id,
               variant.mode,
               variant.word_count,
               variant.exact_token_ids,
               variant.display_text,
               variant.occurrence_count,
               variant.ayah_count,
               variant.surah_count,
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
          AND variant.id <> @anchor_variant_id
          AND score.matched_count >= @minimum_matched_words
        ORDER BY score.matched_count DESC,
                 variant.id
        OFFSET @offset
        LIMIT @page_size
        """;

    public async Task<PhraseSearchReadResult<PhraseSimilaritySearchResponse>> SearchAsync(
        PhraseResolutionReference resolution,
        short minimumMatchedWords,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.BuildChanged();
        }

        var anchor = await LoadVariantAsync(
            snapshot.ActiveBuildId,
            resolution,
            cancellationToken);
        if (anchor is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.InvalidReference();
        }

        int totalCount;
        IReadOnlyList<SimilarityMatchRow> rows;
        if (anchor.WordCount < PhraseSimilarityContract.MinimumGlobalLength)
        {
            totalCount = await CountManualScanAsync(
                snapshot.ActiveBuildId,
                anchor,
                minimumMatchedWords,
                cancellationToken);
            rows = await ReadManualScanPageAsync(
                snapshot.ActiveBuildId,
                anchor,
                minimumMatchedWords,
                CalculateOffset(page, pageSize),
                pageSize,
                cancellationToken);
        }
        else
        {
            totalCount = await CountDirectNeighborsAsync(
                snapshot.ActiveBuildId,
                anchor.Id,
                minimumMatchedWords,
                cancellationToken);
            rows = await ReadDirectNeighborsPageAsync(
                snapshot.ActiveBuildId,
                anchor.Id,
                minimumMatchedWords,
                CalculateOffset(page, pageSize),
                pageSize,
                cancellationToken);
        }

        var response = new PhraseSimilaritySearchResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(anchor.Mode),
            anchor.WordCount,
            minimumMatchedWords,
            page,
            pageSize,
            totalCount,
            ToDto(anchor),
            await CreateMatchesAsync(
                snapshot.ActiveBuildId,
                anchor,
                rows,
                cancellationToken));
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Success(response);
    }

    private async Task<int> CountManualScanAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(ManualScanCountSql);
        AddManualParameters(command, buildId, anchor, minimumMatchedWords);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<SimilarityMatchRow>> ReadManualScanPageAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords,
        long offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(ManualScanPageSql);
        AddManualParameters(command, buildId, anchor, minimumMatchedWords);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, offset);
        command.Parameters.AddWithValue("page_size", pageSize);
        return await ReadMatchRowsAsync(command, cancellationToken);
    }

    private static void AddManualParameters(
        NpgsqlCommand command,
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords)
    {
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, (short)anchor.Mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, anchor.WordCount);
        command.Parameters.AddWithValue("anchor_variant_id", anchor.Id);
        command.Parameters.AddWithValue(
            "exact_token_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Integer,
            anchor.ExactTokenIds);
        command.Parameters.AddWithValue(
            "minimum_matched_words",
            NpgsqlDbType.Smallint,
            minimumMatchedWords);
    }
}
