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

        ValidateLock(artifactLock);
        var artifacts = FullCanonicalArtifactProvisioner.SelectArtifacts(runKind, artifactLock);
        var tables = artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray();
        var ownedSequences = artifacts.SelectMany(artifact => artifact.TableScope.OwnedSequences ?? []).ToArray();
        await source.AssertDisposableRecoverySourceAsync(cancellationToken);

        // All immutable artifact, database-version, migration, count, and critical-read checks finish
        // before sequence state is mutated in the disposable restored source.
        var content = new List<FullCanonicalRecoveredContent>();
        foreach (var artifact in artifacts)
        {
            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await source.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await source.AssertMigrationAsync(artifact.Migration, cancellationToken);
            content.Add(await ReadExpectedContentAsync(artifact, stagingRoot, source, cancellationToken));
        }

        var reconciliations = await source.ReconcileOwnedSequencesAsync(tables, ownedSequences, cancellationToken);
        ValidateReconciliations(ownedSequences, reconciliations);
        var reconciledStates = await source.ReadSequenceStatesAsync(tables, ownedSequences, cancellationToken);
        ValidateSequenceStates(ownedSequences, reconciledStates);
        if (!reconciledStates.SequenceEqual(reconciliations.Select(result => result.Reconciled)))
        {
            throw new InvalidOperationException("Recovery source sequence state changed after transactional reconciliation.");
        }

        var recoveredArtifacts = artifacts.Select((artifact, index) =>
            ToRecoveredArtifact(
                content[index],
                SelectSequenceStates(artifact.TableScope.OwnedSequences ?? [], reconciledStates)))
            .ToArray();

        await source.CreateBackupAsync(
            tables,
            ownedSequences.Select(sequence => sequence.Name).ToArray(),
            backupPath,
            cancellationToken);
        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException("The recovery backup was not produced.");
        }

        return new FullCanonicalRecoveryBackup(
            Path.GetFileName(backupPath),
            new FileInfo(backupPath).Length,
            Sha256(backupPath),
            RepositoryMigrationState.Read(repositoryRoot),
            tables,
            ownedSequences,
            reconciliations,
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
        ValidateLock(artifactLock);
        var artifacts = FullCanonicalArtifactProvisioner.SelectArtifacts(runKind, artifactLock);
        var tables = artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray();
        var ownedSequences = artifacts.SelectMany(artifact => artifact.TableScope.OwnedSequences ?? []).ToArray();
        VerifyBackupIntegrity(backupPath, backup, repositoryRoot);
        if (backup.Artifacts.Count != artifacts.Count
            || !backup.Tables.SequenceEqual(tables, StringComparer.Ordinal)
            || !backup.OwnedSequences.SequenceEqual(ownedSequences))
        {
            throw new InvalidOperationException("The recovery backup artifact set does not match the locked restore contract.");
        }
        ValidateReconciliations(ownedSequences, backup.SequenceReconciliations);

        foreach (var artifact in artifacts)
        {
            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await target.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await target.AssertMigrationAsync(artifact.Migration, cancellationToken);
        }

        await target.AssertDisposableRecoveryTargetAsync(cancellationToken);
        await target.AssertRestoreTargetIsEmptyAsync(tables, cancellationToken);
        // Recheck at the last possible point before the only target mutation.
        VerifyBackupIntegrity(backupPath, backup, repositoryRoot);
        await target.RestoreBackupAsync(
            tables,
            ownedSequences.Select(sequence => sequence.Name).ToArray(),
            backupPath,
            cancellationToken);

        for (var index = 0; index < artifacts.Count; index++)
        {
            var content = await ReadExpectedContentAsync(artifacts[index], stagingRoot, target, cancellationToken);
            var sequenceStates = await target.ReadSequenceStatesAsync(
                artifacts[index].TableScope.Tables,
                artifacts[index].TableScope.OwnedSequences ?? [],
                cancellationToken);
            ValidateSequenceStates(artifacts[index].TableScope.OwnedSequences ?? [], sequenceStates);
            var actual = ToRecoveredArtifact(content, sequenceStates);
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

    private static async Task<FullCanonicalRecoveredContent> ReadExpectedContentAsync(
        LockedArtifact artifact,
        string stagingRoot,
        IFullCanonicalRecoveryDatabase database,
        CancellationToken cancellationToken)
    {
        var manifest = ArtifactManifestReader.ReadTables(artifact, Path.Combine(stagingRoot, artifact.ManifestPath));
        var counts = await database.CountRowsAsync(manifest.Select(table => table.Name).ToArray(), cancellationToken);
        foreach (var table in manifest)
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
        if (sentinels.Any(sentinel => sentinel.CriticalReadSha256 is null))
        {
            throw new InvalidOperationException("Recovery verification requires locked critical-read fingerprints.");
        }

        var criticalReads = await database.ReadCriticalFingerprintsAsync(sentinels, cancellationToken);
        if (criticalReads.Count != sentinels.Count
            || sentinels.Any(sentinel => !criticalReads.ContainsKey(sentinel.Id)
                || !IsSha256(criticalReads[sentinel.Id])
                || !string.Equals(criticalReads[sentinel.Id], sentinel.CriticalReadSha256, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Recovery verification critical-read fingerprint does not match the locked contract.");
        }

        return new FullCanonicalRecoveredContent(
            artifact.Id,
            artifact.ImmutableStorageId,
            manifest,
            results,
            artifact.StagedFiles,
            artifact.Sources,
            criticalReads.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new FullCanonicalCriticalRead(entry.Key, entry.Value))
                .ToArray());
    }

    private static FullCanonicalRecoveredArtifact ToRecoveredArtifact(
        FullCanonicalRecoveredContent content,
        IReadOnlyList<FullCanonicalSequenceState> sequences) =>
        new(
            content.Id,
            content.ImmutableStorageId,
            content.Tables,
            content.Sentinels,
            content.StagedFiles,
            content.Sources,
            content.CriticalReads,
            sequences);

    private static IReadOnlyList<FullCanonicalSequenceState> SelectSequenceStates(
        IReadOnlyList<ArtifactOwnedSequence> expected,
        IReadOnlyList<FullCanonicalSequenceState> actual)
    {
        var byName = actual.ToDictionary(state => state.Name, StringComparer.Ordinal);
        return expected.Select(sequence => byName[sequence.Name]).ToArray();
    }

    private static void ValidateReconciliations(
        IReadOnlyList<ArtifactOwnedSequence> expected,
        IReadOnlyList<FullCanonicalSequenceReconciliation> actual)
    {
        if (!actual.Select(result => result.Original.Ownership).SequenceEqual(expected)
            || !actual.Select(result => result.Reconciled.Ownership).SequenceEqual(expected))
        {
            throw new InvalidOperationException("Recovery sequence reconciliation does not exactly match the locked ownership contract.");
        }

        for (var index = 0; index < actual.Count; index++)
        {
            if (actual[index].Original.HighWaterMark != actual[index].Reconciled.HighWaterMark)
            {
                throw new InvalidOperationException("Recovery sequence reconciliation changed the owned table high-water mark.");
            }
        }

        ValidateSequenceStates(expected, actual.Select(result => result.Reconciled).ToArray());
    }

    private static void ValidateSequenceStates(
        IReadOnlyList<ArtifactOwnedSequence> expected,
        IReadOnlyList<FullCanonicalSequenceState> actual)
    {
        if (!actual.Select(state => state.Ownership).SequenceEqual(expected))
        {
            throw new InvalidOperationException("Recovery sequence state does not exactly match the locked ownership contract.");
        }

        if (actual.Any(state => state.IncrementBy <= 0
            || state.HighWaterMark is long highWaterMark && state.NextValue <= highWaterMark))
        {
            throw new InvalidOperationException("Recovery sequence next value is not strictly above its owned table high-water mark.");
        }
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
            && actual.CriticalReads.SequenceEqual(expected.CriticalReads)
            && actual.Sequences.SequenceEqual(expected.Sequences);
    }

    private static void ValidateLock(ArtifactTrustLock artifactLock)
    {
        var issue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (issue is not null)
        {
            throw new InvalidOperationException($"Recovery artifact lock is invalid: {issue}");
        }
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

    private sealed record FullCanonicalRecoveredContent(
        string Id,
        string ImmutableStorageId,
        IReadOnlyList<ArtifactManifestTable> Tables,
        IReadOnlyList<FullCanonicalSentinelResult> Sentinels,
        IReadOnlyList<LockedArtifactFile> StagedFiles,
        IReadOnlyList<ArtifactSource> Sources,
        IReadOnlyList<FullCanonicalCriticalRead> CriticalReads);
}

internal interface IFullCanonicalRecoveryDatabase : IFullCanonicalArtifactDatabase
{
    Task AssertDisposableRecoverySourceAsync(CancellationToken cancellationToken = default);

    Task AssertDisposableRecoveryTargetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FullCanonicalSequenceReconciliation>> ReconcileOwnedSequencesAsync(
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FullCanonicalSequenceState>> ReadSequenceStatesAsync(
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
        CancellationToken cancellationToken = default);

    Task CreateBackupAsync(
        IReadOnlyList<string> tables,
        IReadOnlyList<string> sequences,
        string backupPath,
        CancellationToken cancellationToken = default);

    Task RestoreBackupAsync(
        IReadOnlyList<string> tables,
        IReadOnlyList<string> sequences,
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
    IReadOnlyList<string> Tables,
    IReadOnlyList<ArtifactOwnedSequence> OwnedSequences,
    IReadOnlyList<FullCanonicalSequenceReconciliation> SequenceReconciliations,
    IReadOnlyList<FullCanonicalRecoveredArtifact> Artifacts);

internal sealed record FullCanonicalRecoveredArtifact(
    string Id,
    string ImmutableStorageId,
    IReadOnlyList<ArtifactManifestTable> Tables,
    IReadOnlyList<FullCanonicalSentinelResult> Sentinels,
    IReadOnlyList<LockedArtifactFile> StagedFiles,
    IReadOnlyList<ArtifactSource> Sources,
    IReadOnlyList<FullCanonicalCriticalRead> CriticalReads,
    IReadOnlyList<FullCanonicalSequenceState> Sequences);

internal sealed record FullCanonicalCriticalRead(string Id, string Sha256);

internal sealed record FullCanonicalSequenceState(
    ArtifactOwnedSequence Ownership,
    long? HighWaterMark,
    long LastValue,
    bool IsCalled,
    long IncrementBy,
    long NextValue)
{
    internal string Name => Ownership.Name;
}

internal sealed record FullCanonicalSequenceReconciliation(
    FullCanonicalSequenceState Original,
    FullCanonicalSequenceState Reconciled);

internal sealed record FullCanonicalRecoveryReceipt(
    string Status,
    string Classification,
    string ApplicationRollback,
    FullCanonicalRecoveryBackup Backup);
