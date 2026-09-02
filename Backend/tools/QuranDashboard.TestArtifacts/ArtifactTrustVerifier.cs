using System.Security.Cryptography;
using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

internal static class ArtifactTrustVerifier
{
    internal static ArtifactTrustResult Status(
        LockedArtifact artifact,
        string repositoryRoot)
    {
        var stagedFiles = InspectStagedFiles(artifact, repositoryRoot, verifyHashes: false);
        return stagedFiles.State == ArtifactTrustState.Present
            ? CheckMigrationState(artifact, repositoryRoot)
            : stagedFiles;
    }

    internal static ArtifactTrustResult Verify(
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact,
        string repositoryRoot,
        string? stagedRoot = null)
    {
        var artifactRoot = stagedRoot ?? repositoryRoot;
        var stagedFiles = InspectStagedFiles(artifact, artifactRoot, verifyHashes: true);
        if (stagedFiles.State != ArtifactTrustState.Present)
        {
            return stagedFiles;
        }

        var manifestPath = Path.Combine(artifactRoot, artifact.ManifestPath);
        var mismatch = CompareArtifactManifest(artifactLock, artifact, manifestPath);
        if (mismatch is not null)
        {
            return new ArtifactTrustResult(ArtifactTrustState.Mismatched, mismatch);
        }

        var migrationState = CheckMigrationState(artifact, repositoryRoot);
        return migrationState.State == ArtifactTrustState.Present
            ? new ArtifactTrustResult(ArtifactTrustState.Present, "verified")
            : migrationState;
    }

    internal static ArtifactTrustResult VerifyContentAddressed(
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact,
        string repositoryRoot,
        string contentAddressedRoot)
    {
        var payload = artifact.StagedFiles.Single(file => file.Role == "payload");
        if (!artifact.ImmutableStorageId.StartsWith("local://", StringComparison.Ordinal)
            || !artifact.ImmutableStorageId.EndsWith($"@sha256:{payload.Sha256}", StringComparison.Ordinal))
        {
            return new ArtifactTrustResult(
                ArtifactTrustState.Mismatched,
                "local immutable storage identity does not match the locked payload hash");
        }

        var artifactRoot = Path.Combine(Path.GetFullPath(contentAddressedRoot), "sha256", payload.Sha256);
        foreach (var file in artifact.StagedFiles)
        {
            var source = Path.Combine(artifactRoot, Path.GetFileName(file.Path));
            if (!File.Exists(source))
            {
                return new ArtifactTrustResult(ArtifactTrustState.Missing, "content-addressed staged file is missing");
            }

            if (new FileInfo(source).Length != file.Size || !string.Equals(
                    ComputeSha256(source), file.Sha256, StringComparison.Ordinal))
            {
                return new ArtifactTrustResult(ArtifactTrustState.Mismatched, "content-addressed staged file differs from the lock");
            }
        }

        var manifest = Path.Combine(artifactRoot, Path.GetFileName(artifact.ManifestPath));
        var mismatch = CompareArtifactManifest(artifactLock, artifact, manifest);
        if (mismatch is not null)
        {
            return new ArtifactTrustResult(ArtifactTrustState.Mismatched, mismatch);
        }

        var migrationState = CheckMigrationState(artifact, repositoryRoot);
        return migrationState.State == ArtifactTrustState.Present
            ? new ArtifactTrustResult(ArtifactTrustState.Present, "content-addressed artifact verified")
            : migrationState;
    }

    private static string? CompareArtifactManifest(
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact,
        string manifestPath)
    {
        if (artifact.Restore is null)
        {
            return CompareManifest(artifactLock, artifact, TestArtifactManifest.ReadFrom(manifestPath));
        }

        try
        {
            return CompareCanonicalDumpManifest(artifact, CanonicalDumpManifest.ReadFrom(manifestPath));
        }
        catch (JsonException)
        {
            return CompareManifest(artifactLock, artifact, TestArtifactManifest.ReadFrom(manifestPath));
        }
    }

    private static ArtifactTrustResult InspectStagedFiles(
        LockedArtifact artifact,
        string repositoryRoot,
        bool verifyHashes)
    {
        foreach (var stagedFile in artifact.StagedFiles)
        {
            var fullPath = Path.Combine(repositoryRoot, stagedFile.Path);
            if (!File.Exists(fullPath))
            {
                return new ArtifactTrustResult(
                    ArtifactTrustState.Missing,
                    $"staged file is missing: {stagedFile.Path}");
            }

            var actualSize = new FileInfo(fullPath).Length;
            if (actualSize != stagedFile.Size)
            {
                return new ArtifactTrustResult(
                    ArtifactTrustState.Mismatched,
                    $"size mismatch for {stagedFile.Path}: expected {stagedFile.Size}, actual {actualSize}");
            }

            if (verifyHashes)
            {
                var actualSha256 = ComputeSha256(fullPath);
                if (!string.Equals(actualSha256, stagedFile.Sha256, StringComparison.Ordinal))
                {
                    return new ArtifactTrustResult(
                        ArtifactTrustState.Mismatched,
                        $"sha256 mismatch for {stagedFile.Path}: expected {stagedFile.Sha256}, actual {actualSha256}");
                }
            }
        }

        return new ArtifactTrustResult(
            ArtifactTrustState.Present,
            verifyHashes ? "staged files verified" : "staged files present");
    }

    private static string? CompareManifest(
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact,
        TestArtifactManifest manifest)
    {
        var invalidTable = artifact.TableScope.Tables
            .Concat(manifest.Tables.Select(table => table.Name))
            .FirstOrDefault(table => !ArtifactTrustLockValidator.IsValidTableIdentifier(table));
        if (invalidTable is not null)
        {
            return $"invalid table identifier '{invalidTable}'";
        }

        if (manifest.ContractVersion != artifactLock.ContractVersion)
        {
            return $"manifest contractVersion {manifest.ContractVersion} does not match lock contractVersion {artifactLock.ContractVersion}";
        }

        if (!string.Equals(manifest.ArtifactId, artifact.Id, StringComparison.Ordinal)
            || !string.Equals(manifest.ArtifactVersion, artifact.Version, StringComparison.Ordinal))
        {
            return "manifest artifact identity/version does not match the lock";
        }

        if (manifest.Migration != artifact.Migration)
        {
            return "manifest migration state does not match the lock";
        }

        if (!string.Equals(
                manifest.PostgreSql.ProducerVersion,
                artifact.PostgreSql.ProducerVersion,
                StringComparison.Ordinal))
        {
            return "manifest PostgreSQL producer version does not match the lock";
        }

        if (manifest.Producer != artifact.Producer)
        {
            return "manifest producer does not match the lock";
        }

        if (!manifest.Tables.Select(table => table.Name).SequenceEqual(
                artifact.TableScope.Tables,
                StringComparer.Ordinal))
        {
            return "manifest table scope does not match the lock";
        }

        if (manifest.Tables.Any(table => table.Rows < 0)
            || manifest.Tables.Select(table => table.Name).Distinct(StringComparer.Ordinal).Count()
            != manifest.Tables.Count)
        {
            return "manifest table rows or identifiers are invalid";
        }

        if (artifact.TableCounts is not null && !manifest.Tables.SequenceEqual(artifact.TableCounts))
        {
            return "manifest table counts do not match the lock";
        }

        if (!manifest.Sources.SequenceEqual(artifact.Sources)
            || !manifest.Sentinels.SequenceEqual(artifact.Sentinels))
        {
            return "manifest source provenance or sentinels do not match the lock";
        }

        if (artifact.PhraseSearch is null && manifest.PhraseSearch is not null)
        {
            return "manifest carries PhraseSearch state not declared by the lock";
        }

        if (artifact.PhraseSearch is not null)
        {
            if (manifest.PhraseSearch is null
                || string.IsNullOrWhiteSpace(manifest.PhraseSearch.ActiveBuildId)
                || !string.Equals(
                    manifest.PhraseSearch.SourceFingerprint,
                    artifact.PhraseSearch.SourceFingerprint,
                    StringComparison.Ordinal)
                || !string.Equals(
                    manifest.PhraseSearch.Readiness,
                    artifact.PhraseSearch.ReadinessExpectation,
                    StringComparison.Ordinal))
            {
                return "manifest PhraseSearch state does not match the lock";
            }

            var manifestFile = artifact.StagedFiles.Single(
                file => string.Equals(file.Path, artifact.ManifestPath, StringComparison.Ordinal));
            if (!string.Equals(
                    manifestFile.Sha256,
                    artifact.PhraseSearch.ManifestSha256,
                    StringComparison.Ordinal))
            {
                return "PhraseSearch manifest hash does not match the staged manifest hash";
            }
        }

        if (!RestoreContractsMatch(manifest.Restore, artifact.Restore))
        {
            return "manifest restore contract does not match the lock";
        }

        return null;
    }

    private static string? CompareCanonicalDumpManifest(
        LockedArtifact artifact,
        CanonicalDumpManifest manifest)
    {
        var payload = artifact.StagedFiles.Single(file => file.Role == "payload");
        var tables = manifest.TableCounts();
        if (!string.Equals(manifest.Name, artifact.Id, StringComparison.Ordinal)
            || manifest.MigrationId != artifact.Migration.Head
            || manifest.MigrationCount != artifact.Migration.Count
            || manifest.PgDumpVersion != artifact.PostgreSql.ProducerVersion
            || manifest.DumpSha256 != payload.Sha256)
        {
            return "canonical dump manifest identity, migration, producer, or payload hash does not match the lock";
        }

        if (artifact.TableCounts is null
            || !tables.SequenceEqual(artifact.TableCounts)
            || !tables.Select(table => table.Name).SequenceEqual(artifact.TableScope.Tables, StringComparer.Ordinal)
            || tables.Any(table => table.Rows < 0 || !ArtifactTrustLockValidator.IsValidTableIdentifier(table.Name)))
        {
            return "canonical dump manifest table scope or counts do not match the lock";
        }

        return null;
    }

    private static bool RestoreContractsMatch(
        ArtifactRestoreContract? manifest,
        ArtifactRestoreContract? artifact)
    {
        if (manifest is null || artifact is null)
        {
            return manifest is null && artifact is null;
        }

        return manifest.Kind == artifact.Kind
            && manifest.Order == artifact.Order
            && manifest.SentinelTables.SequenceEqual(artifact.SentinelTables);
    }

    private static ArtifactTrustResult CheckMigrationState(
        LockedArtifact artifact,
        string repositoryRoot)
    {
        var current = RepositoryMigrationState.Read(repositoryRoot);
        return artifact.Migration == current
            ? new ArtifactTrustResult(ArtifactTrustState.Present, "migration state current")
            : new ArtifactTrustResult(
                ArtifactTrustState.Stale,
                $"lock migration {artifact.Migration.Head} (count {artifact.Migration.Count}) " +
                $"does not match repository migration {current.Head} (count {current.Count})");
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}

internal enum ArtifactTrustState
{
    Present,
    Missing,
    Mismatched,
    Stale,
}

internal sealed record ArtifactTrustResult(ArtifactTrustState State, string Detail);

internal static class RepositoryMigrationState
{
    private const string MigrationsRelativePath =
        "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations";

    internal static ArtifactMigrationState Read(string repositoryRoot)
    {
        var migrationsDirectory = Path.Combine(repositoryRoot, MigrationsRelativePath);
        if (!Directory.Exists(migrationsDirectory))
        {
            throw new InvalidDataException(
                $"Repository migrations directory is missing: {migrationsDirectory}.");
        }

        var migrations = Directory
            .EnumerateFiles(migrationsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && ArtifactTrustLockValidator.IsMigrationId(name))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (migrations.Length == 0)
        {
            throw new InvalidDataException(
                $"Repository migrations directory has no migration files: {migrationsDirectory}.");
        }

        return new ArtifactMigrationState(migrations[^1]!, migrations.Length);
    }
}
