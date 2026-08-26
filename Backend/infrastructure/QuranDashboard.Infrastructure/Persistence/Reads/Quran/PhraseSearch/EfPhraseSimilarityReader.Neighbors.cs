using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    private const string DirectNeighborCountSql = """
        SELECT (
          SELECT COUNT(*)
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
            AND left_variant_id = @anchor_variant_id
            AND matched_count >= @minimum_matched_words
        ) + (
          SELECT COUNT(*)
          FROM quran_phrase_similarity_edges
          WHERE build_id = @build_id
            AND right_variant_id = @anchor_variant_id
            AND matched_count >= @minimum_matched_words
        )
        """;

    private const string DirectNeighborPageSql = """
        WITH direct_neighbors AS (
          (
            SELECT right_variant_id AS variant_id,
                   matched_count
            FROM quran_phrase_similarity_edges
            WHERE build_id = @build_id
              AND left_variant_id = @anchor_variant_id
              AND matched_count >= @minimum_matched_words
            ORDER BY matched_count DESC, right_variant_id
          )
          UNION ALL
          (
            SELECT left_variant_id AS variant_id,
                   matched_count
            FROM quran_phrase_similarity_edges
            WHERE build_id = @build_id
              AND right_variant_id = @anchor_variant_id
              AND matched_count >= @minimum_matched_words
            ORDER BY matched_count DESC, left_variant_id
          )
        )
        SELECT variant.id,
               variant.mode,
               variant.word_count,
               variant.exact_token_ids,
               variant.display_text,
               variant.occurrence_count,
               variant.ayah_count,
               variant.surah_count,
               neighbor.matched_count
        FROM direct_neighbors AS neighbor
        JOIN quran_phrase_variants AS variant
          ON variant.build_id = @build_id
         AND variant.id = neighbor.variant_id
        ORDER BY neighbor.matched_count DESC,
                 neighbor.variant_id
        OFFSET @offset
        LIMIT @page_size
        """;

    private async Task<int> CountDirectNeighborsAsync(
        Guid buildId,
        long anchorVariantId,
        short minimumMatchedWords,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(DirectNeighborCountSql);
        AddNeighborParameters(command, buildId, anchorVariantId, minimumMatchedWords);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<SimilarityMatchRow>> ReadDirectNeighborsPageAsync(
        Guid buildId,
        long anchorVariantId,
        short minimumMatchedWords,
        long offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(DirectNeighborPageSql);
        AddNeighborParameters(command, buildId, anchorVariantId, minimumMatchedWords);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, offset);
        command.Parameters.AddWithValue("page_size", pageSize);
        return await ReadMatchRowsAsync(command, cancellationToken);
    }

    private NpgsqlCommand CreateCommand(string sql)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("PhraseSearch similarity read requires an active snapshot transaction.");
        return new NpgsqlCommand(sql, connection, transaction);
    }

    private static void AddNeighborParameters(
        NpgsqlCommand command,
        Guid buildId,
        long anchorVariantId,
        short minimumMatchedWords)
    {
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("anchor_variant_id", anchorVariantId);
        command.Parameters.AddWithValue(
            "minimum_matched_words",
            NpgsqlDbType.Smallint,
            minimumMatchedWords);
    }

    private static async Task<IReadOnlyList<SimilarityMatchRow>> ReadMatchRowsAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var rows = new List<SimilarityMatchRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var variant = new SimilarityVariantRow(
                reader.GetInt64(0),
                (PhraseTextMode)reader.GetInt16(1),
                reader.GetInt16(2),
                reader.GetFieldValue<int[]>(3),
                reader.GetString(4),
                reader.GetInt64(5),
                reader.GetInt32(6),
                reader.GetInt16(7));
            rows.Add(new SimilarityMatchRow(
                variant,
                reader.GetInt16(8)));
        }

        return rows;
    }
}
