using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

internal static class NavigationMetadataBulkCopier
{
    public static async Task CopyNavigationDataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AssembledNavigationMetadata assembled,
        CancellationToken ct)
    {
        await CopyJuzRowsAsync(connection, transaction, assembled.Juz, ct);
        await CopyHizbRowsAsync(connection, transaction, assembled.Hizb, ct);
        await CopyRubRowsAsync(connection, transaction, assembled.Rub, ct);
        await CopySajdaRowsAsync(connection, transaction, assembled.Sajda, ct);
        await CopyAyahNavigationUpdatesAsync(connection, transaction, assembled.AyahAssignments, ct);
    }

    private static async Task CopyJuzRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<AssembledDivision> rows,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_juzs (
                juz_number,
                verses_count,
                first_ayah_id,
                last_ayah_id,
                first_verse_key,
                last_verse_key)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(row.Number, ct);
            await importer.WriteAsync(row.VersesCount, ct);
            await importer.WriteAsync(row.FirstAyahId, ct);
            await importer.WriteAsync(row.LastAyahId, ct);
            await importer.WriteAsync(row.FirstVerseKey, ct);
            await importer.WriteAsync(row.LastVerseKey, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyHizbRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<AssembledDivision> rows,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_hizbs (
                hizb_number,
                juz_number,
                verses_count,
                first_ayah_id,
                last_ayah_id,
                first_verse_key,
                last_verse_key)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(row.Number, ct);
            await importer.WriteAsync(row.ParentJuzNumber ?? throw new InvalidOperationException("Hizb parent juz was not derived."), ct);
            await importer.WriteAsync(row.VersesCount, ct);
            await importer.WriteAsync(row.FirstAyahId, ct);
            await importer.WriteAsync(row.LastAyahId, ct);
            await importer.WriteAsync(row.FirstVerseKey, ct);
            await importer.WriteAsync(row.LastVerseKey, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyRubRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<AssembledDivision> rows,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_rubs (
                rub_number,
                hizb_number,
                verses_count,
                first_ayah_id,
                last_ayah_id,
                first_verse_key,
                last_verse_key)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(row.Number, ct);
            await importer.WriteAsync(row.ParentHizbNumber ?? throw new InvalidOperationException("Rub parent hizb was not derived."), ct);
            await importer.WriteAsync(row.VersesCount, ct);
            await importer.WriteAsync(row.FirstAyahId, ct);
            await importer.WriteAsync(row.LastAyahId, ct);
            await importer.WriteAsync(row.FirstVerseKey, ct);
            await importer.WriteAsync(row.LastVerseKey, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopySajdaRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<AssembledSajda> rows,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_sajdas (
                sajdah_number,
                ayah_id,
                verse_key,
                sajdah_type)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(row.SajdahNumber, ct);
            await importer.WriteAsync(row.AyahId, ct);
            await importer.WriteAsync(row.VerseKey, ct);
            await importer.WriteAsync(ToDatabaseSajdaType(row.SajdahType), ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyAyahNavigationUpdatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyDictionary<int, AyahNavigationAssignment> assignments,
        CancellationToken ct)
    {
        await NavigationMetadataCommandExecutor.ExecuteNonQueryAsync(
            connection,
            transaction,
            NavigationMetadataSql.CreateAyahUpdateTempTable,
            ct);

        const string copyCommand = """
            COPY nav_ayah_updates (
                ayah_id,
                juz_number,
                hizb_number,
                rub_number)
            FROM STDIN (FORMAT BINARY)
            """;

        await using (var importer = await connection.BeginBinaryImportAsync(copyCommand, ct))
        {
            foreach (var assignment in assignments.Values.OrderBy(item => item.AyahId))
            {
                await importer.StartRowAsync(ct);
                await importer.WriteAsync(assignment.AyahId, ct);
                await importer.WriteAsync(assignment.JuzNumber, ct);
                await importer.WriteAsync(assignment.HizbNumber, ct);
                await importer.WriteAsync(assignment.RubNumber, ct);
            }

            await importer.CompleteAsync(ct);
        }

        await NavigationMetadataCommandExecutor.ExecuteNonQueryAsync(
            connection,
            transaction,
            NavigationMetadataSql.ApplyAyahNavigationUpdates,
            ct);
    }

    private static string ToDatabaseSajdaType(SajdahType value) => value switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown sajdah type.")
    };
}
