using System.Security.Cryptography;

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
        string repositoryRoot)
    {
        var stagedFiles = InspectStagedFiles(artifact, repositoryRoot, verifyHashes: true);
        if (stagedFiles.State != ArtifactTrustState.Present)
        {
            return stagedFiles;
        }

        var manifest = TestArtifactManifest.ReadFrom(
            Path.Combine(repositoryRoot, artifact.ManifestPath));
        var mismatch = CompareManifest(artifactLock, artifact, manifest);
        if (mismatch is not null)
        {
            return new ArtifactTrustResult(ArtifactTrustState.Mismatched, mismatch);
        }

        var migrationState = CheckMigrationState(artifact, repositoryRoot);
        return migrationState.State == ArtifactTrustState.Present
            ? new ArtifactTrustResult(ArtifactTrustState.Present, "verified")
            : migrationState;
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

        return null;
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
