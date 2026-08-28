using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseDatabaseStoragePreflight
{
    private readonly PhraseIndexOptions options;

    public PhraseDatabaseStoragePreflight(IOptions<PhraseIndexOptions> options)
    {
        this.options = options.Value;
    }

    internal async Task<PhraseDiskPreflight> ReadAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        const string sql = """
            SELECT pg_database_size(current_database()),
                   (
                     SELECT COALESCE(SUM(pg_total_relation_size(class.oid)), 0)::bigint
                     FROM pg_class AS class
                     JOIN pg_namespace AS namespace ON namespace.oid = class.relnamespace
                       AND namespace.nspname = 'public'
                     WHERE class.relkind = 'r'
                       AND class.relname LIKE 'quran_phrase_%'
                   )
            """;
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var databaseBytes = reader.GetInt64(0);
        var phraseBytes = reader.GetInt64(1);
        var storageProof = ResolveStorageProof();
        var additionalGenerationBytes = databaseBytes;
        var walHeadroomBytes = databaseBytes;
        var requiredBytes = checked(
            additionalGenerationBytes
            + walHeadroomBytes
            + options.DiskSafetyBytes);

        return new PhraseDiskPreflight(
            databaseBytes,
            phraseBytes,
            additionalGenerationBytes,
            walHeadroomBytes,
            options.DiskSafetyBytes,
            storageProof.AvailableBytes,
            requiredBytes,
            storageProof.Kind,
            storageProof.Verified,
            storageProof.Verified && storageProof.AvailableBytes >= requiredBytes);
    }

    private StorageProof ResolveStorageProof()
    {
        if (options.VerifiedDatabaseFreeBytes is > 0
            && string.Equals(
                options.DatabaseStorageProofContract,
                PhraseIndexOptions.OperatorStorageProofContract,
                StringComparison.Ordinal))
        {
            return new StorageProof(
                options.VerifiedDatabaseFreeBytes.Value,
                PhraseIndexOptions.OperatorStorageProofContract,
                true);
        }

        return new StorageProof(0, "remote-database-storage-proof-unavailable", false);
    }

    private sealed record StorageProof(long AvailableBytes, string Kind, bool Verified);
}
