using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

internal static class MutashabihatBulkCopier
{
    public static async Task<Dictionary<int, int>> CopyGroupsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MutashabihatSourceData source,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_mutashabihat_groups (
                source_group_id,
                representative_ayah_id,
                representative_word_from,
                representative_word_to,
                occurrence_count,
                distinct_ayah_count,
                distinct_surah_count,
                raw_source_counts)
            FROM STDIN (FORMAT BINARY)
            """;

        await using (var importer = await connection.BeginBinaryImportAsync(copyCommand, ct))
        {
            foreach (var group in source.Groups)
            {
                await importer.StartRowAsync(ct);
                await importer.WriteAsync(group.SourceGroupId, ct);
                await importer.WriteAsync(group.RepresentativeAyahId, ct);
                await importer.WriteAsync(group.RepresentativeWordFrom, ct);
                await importer.WriteAsync(group.RepresentativeWordTo, ct);
                await importer.WriteAsync(group.OccurrenceCount, ct);
                await importer.WriteAsync(group.DistinctAyahCount, ct);
                await importer.WriteAsync(group.DistinctSurahCount, ct);
                await importer.WriteAsync(group.RawSourceCountsJson, NpgsqlDbType.Jsonb, ct);
            }

            await importer.CompleteAsync(ct);
        }

        return await ReadGroupIdsBySourceGroupIdAsync(connection, transaction, ct);
    }

    public static async Task CopyOccurrencesAsync(
        NpgsqlConnection connection,
        MutashabihatSourceData source,
        IReadOnlyDictionary<int, int> groupIdsBySourceGroupId,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_mutashabihat_occurrences (
                group_id,
                ayah_id,
                word_from,
                word_to,
                is_representative)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var group in source.Groups)
        {
            if (!groupIdsBySourceGroupId.TryGetValue(group.SourceGroupId, out var groupId))
            {
                throw new InvalidDataException(
                    $"Group id for source_group_id {group.SourceGroupId} was not found after COPY.");
            }

            foreach (var occurrence in group.Occurrences)
            {
                await importer.StartRowAsync(ct);
                await importer.WriteAsync(groupId, ct);
                await importer.WriteAsync(occurrence.AyahId, ct);
                await importer.WriteAsync(occurrence.WordFrom, ct);
                await importer.WriteAsync(occurrence.WordTo, ct);
                await importer.WriteAsync(occurrence.IsRepresentative, ct);
            }
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopyLinksAsync(
        NpgsqlConnection connection,
        MutashabihatSourceData source,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_similar_ayah_links (
                source_ayah_id,
                target_ayah_id,
                score,
                coverage,
                matched_words_count,
                match_words)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var link in source.Links)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(link.SourceAyahId, ct);
            await importer.WriteAsync(link.TargetAyahId, ct);
            await importer.WriteAsync(link.Score, ct);
            await importer.WriteAsync(link.Coverage, ct);
            await importer.WriteAsync(link.MatchedWordsCount, ct);
            await importer.WriteAsync(link.MatchWordsJson, NpgsqlDbType.Jsonb, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task<Dictionary<int, int>> ReadGroupIdsBySourceGroupIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        const string sql = """
            SELECT source_group_id, id
            FROM quran_mutashabihat_groups
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var map = new Dictionary<int, int>();
        while (await reader.ReadAsync(ct))
        {
            map[reader.GetInt32(0)] = reader.GetInt32(1);
        }

        return map;
    }
}
