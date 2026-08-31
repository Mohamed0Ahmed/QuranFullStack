using System.Security.Cryptography;

namespace QuranDashboard.TestArtifacts;

// This is intentionally provider-neutral: callers own the backup and database adapters while the
// rehearsal owns the fail-closed ordering and the credential-free evidence contract.
internal static class FullCanonicalRecoveryRehearsal
{
    internal static async Task<FullCanonicalRecoveryBackup> CaptureAsync(
        bool confirmBackup,
        string runKind,
        ArtifactTrustLock artifactLock,
        string repositoryRoot,
        string stagingRoot,
        string backupPath,
        IFullCanonicalRecoveryDatabase source,
        CancellationToken cancellationToken = default)
    {
        if (!confirmBackup)
        {
            throw new InvalidOperationException("Recovery backup creation requires explicit operator intent.");
        }

        if (FullCanonicalArtifactProvisioningCommand.IsAtOrBelow(backupPath, repositoryRoot)
            || File.Exists(backupPath)
            || Directory.Exists(backupPath))
        {
            throw new InvalidOperationException("Recovery backup output must be a new private file outside the repository worktree.");
        }

        var artifacts = FullCanonicalArtifactProvisioner.SelectArtifacts(runKind, artifactLock);
        await source.AssertDisposableRecoverySourceAsync(cancellationToken);
        var recoveredArtifacts = new List<FullCanonicalRecoveredArtifact>();
        foreach (var artifact in artifacts)
        {
            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await source.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await source.AssertMigrationAsync(artifact.Migration, cancellationToken);
            recoveredArtifacts.Add(await ReadExpectedStateAsync(artifact, stagingRoot, source, cancellationToken));
        }

        await source.CreateBackupAsync(artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray(), backupPath, cancellationToken);
        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException("The recovery backup was not produced.");
        }

        return new FullCanonicalRecoveryBackup(
            Path.GetFileName(backupPath),
            new FileInfo(backupPath).Length,
            Sha256(backupPath),
            RepositoryMigrationState.Read(repositoryRoot),
            recoveredArtifacts);
    }

    internal static async Task<FullCanonicalRecoveryReceipt> RestoreAsync(
        string runKind,
        ArtifactTrustLock artifactLock,
        string repositoryRoot,
        string stagingRoot,
        string backupPath,
        FullCanonicalRecoveryBackup backup,
        IFullCanonicalRecoveryDatabase target,
        CancellationToken cancellationToken = default)
    {
        var artifacts = FullCanonicalArtifactProvisioner.SelectArtifacts(runKind, artifactLock);
        VerifyBackupIntegrity(backupPath, backup, repositoryRoot);
        if (backup.Artifacts.Count != artifacts.Count)
        {
            throw new InvalidOperationException("The recovery backup artifact set does not match the locked restore contract.");
        }

        foreach (var artifact in artifacts)
        {
            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await target.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await target.AssertMigrationAsync(artifact.Migration, cancellationToken);
        }

        await target.AssertDisposableRecoveryTargetAsync(cancellationToken);
        await target.AssertRestoreTargetIsEmptyAsync(
            artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray(),
            cancellationToken);
        await target.RestoreBackupAsync(artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray(), backupPath, cancellationToken);

        for (var index = 0; index < artifacts.Count; index++)
        {
            var actual = await ReadExpectedStateAsync(artifacts[index], stagingRoot, target, cancellationToken);
            if (!RecoveredArtifactsMatch(actual, backup.Artifacts[index]))
            {
                throw new InvalidOperationException(
                    $"The isolated recovery target does not match backup evidence for artifact '{artifacts[index].Id}'.");
            }
        }

        return new FullCanonicalRecoveryReceipt(
            "rehearsed",
            "data-recovery",
            "application-rollback-not-requested",
            backup);
    }

    private static async Task<FullCanonicalRecoveredArtifact> ReadExpectedStateAsync(
        LockedArtifact artifact,
        string stagingRoot,
        IFullCanonicalRecoveryDatabase database,
        CancellationToken cancellationToken)
    {
        var manifest = TestArtifactManifest.ReadFrom(Path.Combine(stagingRoot, artifact.ManifestPath));
        var counts = await database.CountRowsAsync(manifest.Tables.Select(table => table.Name).ToArray(), cancellationToken);
        foreach (var table in manifest.Tables)
        {
            if (!counts.TryGetValue(table.Name, out var actual) || actual != table.Rows)
            {
                throw new InvalidOperationException(
                    $"Recovery verification row count mismatch for '{artifact.Id}' table '{table.Name}'.");
            }
        }

        var sentinels = artifact.Restore!.SentinelTables;
        var sentinelCounts = await database.CountRowsAsync(
            sentinels.Select(sentinel => sentinel.Table).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        var results = sentinels.Select(sentinel =>
        {
            var actual = sentinelCounts[sentinel.Table];
            if (actual != sentinel.ExpectedCount)
            {
                throw new InvalidOperationException(
                    $"Recovery verification sentinel mismatch for '{artifact.Id}' sentinel '{sentinel.Id}'.");
            }

            return new FullCanonicalSentinelResult(sentinel.Id, sentinel.Table, sentinel.ExpectedCount, actual);
        }).ToArray();
        var criticalReads = await database.ReadCriticalFingerprintsAsync(sentinels, cancellationToken);
        if (criticalReads.Count != sentinels.Count
            || sentinels.Any(sentinel => !criticalReads.ContainsKey(sentinel.Id)
                || !IsSha256(criticalReads[sentinel.Id])
                || !string.Equals(criticalReads[sentinel.Id], sentinel.CriticalReadSha256, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Recovery verification critical-read fingerprint does not match the locked contract.");
        }

        return new FullCanonicalRecoveredArtifact(
            artifact.Id,
            artifact.ImmutableStorageId,
            manifest.Tables,
            results,
            artifact.StagedFiles,
            artifact.Sources,
            criticalReads.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new FullCanonicalCriticalRead(entry.Key, entry.Value))
                .ToArray());
    }

    private static void VerifyArtifactTrust(
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact,
        string repositoryRoot,
        string stagingRoot)
    {
        var trust = ArtifactTrustVerifier.Verify(artifactLock, artifact, repositoryRoot, stagingRoot);
        if (trust.State != ArtifactTrustState.Present)
        {
            throw new InvalidOperationException($"Recovery artifact trust verification failed for '{artifact.Id}': {trust.Detail}");
        }
    }

    private static bool RecoveredArtifactsMatch(
        FullCanonicalRecoveredArtifact actual,
        FullCanonicalRecoveredArtifact expected)
    {
        return actual.Id == expected.Id
            && actual.ImmutableStorageId == expected.ImmutableStorageId
            && actual.Tables.SequenceEqual(expected.Tables)
            && actual.Sentinels.SequenceEqual(expected.Sentinels)
            && actual.StagedFiles.SequenceEqual(expected.StagedFiles)
            && actual.Sources.SequenceEqual(expected.Sources)
            && actual.CriticalReads.SequenceEqual(expected.CriticalReads);
    }

    private static void VerifyBackupIntegrity(
        string backupPath,
        FullCanonicalRecoveryBackup backup,
        string repositoryRoot)
    {
        if (!File.Exists(backupPath)
            || !string.Equals(Path.GetFileName(backupPath), backup.FileName, StringComparison.Ordinal)
            || new FileInfo(backupPath).Length != backup.Size
            || !string.Equals(Sha256(backupPath), backup.Sha256, StringComparison.Ordinal)
            || backup.RepositoryMigration != RepositoryMigrationState.Read(repositoryRoot))
        {
            throw new InvalidOperationException("Recovery backup integrity metadata does not match the backup before restore.");
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64
            && value.All(character => character is >= '0' and <= '9' || character is >= 'a' and <= 'f');
    }
}

internal interface IFullCanonicalRecoveryDatabase : IFullCanonicalArtifactDatabase
{
    Task AssertDisposableRecoverySourceAsync(CancellationToken cancellationToken = default);

    Task AssertDisposableRecoveryTargetAsync(CancellationToken cancellationToken = default);

    Task CreateBackupAsync(
        IReadOnlyList<string> tables,
        string backupPath,
        CancellationToken cancellationToken = default);

    Task RestoreBackupAsync(
        IReadOnlyList<string> tables,
        string backupPath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> ReadCriticalFingerprintsAsync(
        IReadOnlyList<ArtifactRestoreSentinel> sentinels,
        CancellationToken cancellationToken = default);
}

internal sealed record FullCanonicalRecoveryBackup(
    string FileName,
    long Size,
    string Sha256,
    ArtifactMigrationState RepositoryMigration,
    IReadOnlyList<FullCanonicalRecoveredArtifact> Artifacts);

internal sealed record FullCanonicalRecoveredArtifact(
    string Id,
    string ImmutableStorageId,
    IReadOnlyList<ArtifactManifestTable> Tables,
    IReadOnlyList<FullCanonicalSentinelResult> Sentinels,
    IReadOnlyList<LockedArtifactFile> StagedFiles,
    IReadOnlyList<ArtifactSource> Sources,
    IReadOnlyList<FullCanonicalCriticalRead> CriticalReads);

internal sealed record FullCanonicalCriticalRead(string Id, string Sha256);

internal sealed record FullCanonicalRecoveryReceipt(
    string Status,
    string Classification,
    string ApplicationRollback,
    FullCanonicalRecoveryBackup Backup);
