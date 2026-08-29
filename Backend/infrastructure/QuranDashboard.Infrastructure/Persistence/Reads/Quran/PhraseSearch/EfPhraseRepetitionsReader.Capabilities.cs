using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseRepetitionsReader
{
    private const string CapabilityLengthsSql = """
        WITH candidate_lengths AS (
          SELECT modes.mode,
                 lengths.word_count::smallint AS word_count
          FROM (VALUES (@simple_mode::smallint), (@tashkil_mode::smallint)) AS modes(mode)
          CROSS JOIN generate_series(1, @maximum_word_count) AS lengths(word_count)
        )
        SELECT candidate.mode,
               candidate.word_count,
               maximum.occurrence_count,
               EXISTS (
                 SELECT 1
                 FROM quran_phrase_similarity_anchor_stats AS stat
                 WHERE stat.build_id = @build_id
                   AND stat.mode = candidate.mode
                   AND stat.word_count = candidate.word_count
                   AND stat.neighbor_count > 0
               ) AS similarity_supported
        FROM candidate_lengths AS candidate
        CROSS JOIN LATERAL (
          SELECT variant.occurrence_count
          FROM quran_phrase_variants AS variant
          WHERE variant.build_id = @build_id
            AND variant.mode = candidate.mode
            AND variant.word_count = candidate.word_count
          ORDER BY variant.occurrence_count DESC,
                   variant.id
          LIMIT 1
        ) AS maximum
        ORDER BY candidate.mode,
                 candidate.word_count
        """;

    public async Task<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>> GetCapabilitiesAsync(
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Unavailable();
        }

        var response = await cache.GetOrLoadCapabilitiesAsync(
            snapshot.ActiveBuildId,
            token => LoadCapabilitiesAsync(snapshot, token),
            cancellationToken);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success(response);
    }

    private async Task<PhraseSearchCapabilitiesResponse> LoadCapabilitiesAsync(
        PhraseSearchReadSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var lengths = await LoadCapabilityLengthsAsync(snapshot.ActiveBuildId, cancellationToken);
        var modes = new[] { PhraseTextMode.Simple, PhraseTextMode.Tashkil }
            .Select(mode => CreateModeCapabilities(mode, lengths, snapshot.SimilarityReady))
            .ToList();

        return new PhraseSearchCapabilitiesResponse(
            snapshot.ActiveBuildId,
            snapshot.ExactReady,
            snapshot.SimilarityReady,
            PhraseTextModeKeys.Simple,
            PhraseSearchPaging.MinimumRepetitionLength,
            PhraseRepetitionSortKeys.Occurrences,
            PhraseSearchPaging.DefaultPageSize,
            PhraseSearchPaging.MaximumPageSize,
            PhraseSearchPaging.MaximumRepetitionPageSize,
            PhraseSearchPaging.MaximumRepetitionOccurrencePageSize,
            PhraseSimilarityContract.Thresholds.Min(),
            [.. PhraseSimilarityContract.Thresholds],
            modes);
    }

    private async Task<IReadOnlyList<PhraseLengthRow>> LoadCapabilityLengthsAsync(
        Guid buildId,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("PhraseSearch capabilities read requires an active snapshot transaction.");
        await using var command = new NpgsqlCommand(CapabilityLengthsSql, connection, transaction);
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("simple_mode", NpgsqlDbType.Smallint, (short)PhraseTextMode.Simple);
        command.Parameters.AddWithValue("tashkil_mode", NpgsqlDbType.Smallint, (short)PhraseTextMode.Tashkil);
        command.Parameters.AddWithValue("maximum_word_count", PhraseSearchPaging.MaximumSourceLength);

        var rows = new List<PhraseLengthRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PhraseLengthRow(
                (PhraseTextMode)reader.GetInt16(0),
                reader.GetInt16(1),
                reader.GetInt64(2),
                reader.GetBoolean(3)));
        }

        return rows;
    }

    private static PhraseTextModeCapabilitiesDto CreateModeCapabilities(
        PhraseTextMode mode,
        IReadOnlyList<PhraseLengthRow> lengths,
        bool similarityReady)
    {
        var modeLengths = lengths
            .Where(row => row.Mode == mode)
            .ToList();
        var supported = modeLengths
            .Select(row => row.WordCount)
            .ToList();
        var repeated = modeLengths
            .Where(row => row.WordCount >= PhraseSearchPaging.MinimumRepetitionLength
                && row.MaximumOccurrenceCount >= 2)
            .Select(row => row.WordCount)
            .ToList();
        var similarityLengths = similarityReady
            ? modeLengths
                .Where(row => row.SimilaritySupported)
                .Select(row => row.WordCount)
                .ToList()
            : [];

        return new PhraseTextModeCapabilitiesDto(
            PhraseTextModeContract.CanonicalKey(mode),
            supported,
            repeated,
            similarityLengths,
            supported.LastOrDefault(),
            repeated.LastOrDefault(),
            similarityLengths.LastOrDefault());
    }

    private sealed record PhraseLengthRow(
        PhraseTextMode Mode,
        short WordCount,
        long MaximumOccurrenceCount,
        bool SimilaritySupported);
}
