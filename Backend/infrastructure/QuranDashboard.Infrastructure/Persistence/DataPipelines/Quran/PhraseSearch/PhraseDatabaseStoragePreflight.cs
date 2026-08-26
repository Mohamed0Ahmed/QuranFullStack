using System.Net;
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
                   ),
                   COALESCE(inet_server_addr()::text, ''),
                   current_setting('data_directory')
            """;
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var databaseBytes = reader.GetInt64(0);
        var phraseBytes = reader.GetInt64(1);
        var serverAddress = reader.GetString(2);
        var dataDirectory = reader.GetString(3);
        var storageProof = ResolveStorageProof(connection.Host ?? string.Empty, serverAddress, dataDirectory);
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

    private StorageProof ResolveStorageProof(
        string configuredHost,
        string serverAddress,
        string dataDirectory)
    {
        if (IsLocalDatabase(configuredHost, serverAddress))
        {
            return ResolveLocalStorageProof(dataDirectory);
        }

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

    private static bool IsLocalDatabase(string configuredHost, string serverAddress)
    {
        if (string.Equals(configuredHost, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuredHost, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(configuredHost, "::1", StringComparison.Ordinal)
            || Path.IsPathRooted(configuredHost))
        {
            return true;
        }

        return IPAddress.TryParse(serverAddress, out var address) && IPAddress.IsLoopback(address);
    }

    private static StorageProof ResolveLocalStorageProof(string dataDirectory)
    {
        var fullDataDirectory = Path.GetFullPath(dataDirectory);
        var drive = DriveInfo.GetDrives()
            .Where(candidate => candidate.IsReady)
            .Where(candidate => IsWithinDrive(fullDataDirectory, candidate.RootDirectory.FullName))
            .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
            .FirstOrDefault();
        return drive is null
            ? new StorageProof(0, "local-postgresql-data-filesystem-unavailable", false)
            : new StorageProof(
                drive.AvailableFreeSpace,
                "local-postgresql-data-filesystem",
                true);
    }

    private static bool IsWithinDrive(string path, string driveRoot)
    {
        var normalizedRoot = Path.GetFullPath(driveRoot);
        if (string.Equals(path, normalizedRoot, StringComparison.Ordinal))
        {
            return true;
        }

        var rootedPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;
        return path.StartsWith(rootedPrefix, StringComparison.Ordinal);
    }

    private sealed record StorageProof(long AvailableBytes, string Kind, bool Verified);
}
