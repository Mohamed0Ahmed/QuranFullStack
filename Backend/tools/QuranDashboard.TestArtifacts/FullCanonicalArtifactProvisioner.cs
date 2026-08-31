namespace QuranDashboard.TestArtifacts;

// Controlled provisioning owns mutation. The sealed execution path can only re-verify the receipt and
// database state; it has no fetcher or restore operation to call.
internal static class FullCanonicalArtifactProvisioner
{
    internal static void EnsureApplicableArtifacts(string runKind, ArtifactTrustLock artifactLock)
    {
        _ = SelectArtifacts(runKind, artifactLock);
    }

    internal static async Task<FullCanonicalProvisioningReceipt> ProvisionAsync(
        string runKind,
        ArtifactTrustLock artifactLock,
        string repositoryRoot,
        string stagingRoot,
        IFullCanonicalArtifactFetcher fetcher,
        IFullCanonicalArtifactDatabase database,
        CancellationToken cancellationToken = default)
    {
        var artifacts = SelectArtifacts(runKind, artifactLock);
        await PreflightRestoreTargetAsync(artifacts, database, cancellationToken);
        Directory.CreateDirectory(stagingRoot);

        var provisioned = new List<FullCanonicalProvisionedArtifact>();
        foreach (var artifact in artifacts)
        {
            await fetcher.FetchAsync(artifact, stagingRoot, cancellationToken);
            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await database.RestoreAsync(
                artifact,
                Path.Combine(stagingRoot, Payload(artifact).Path),
                cancellationToken);
            provisioned.Add(await VerifyRestoredArtifactAsync(
                artifact,
                stagingRoot,
                database,
                cancellationToken));
        }

        return new FullCanonicalProvisioningReceipt(
            "provisioned",
            runKind,
            RepositoryMigrationState.Read(repositoryRoot),
            provisioned);
    }

    internal static async Task VerifyProvisionedStateAsync(
        FullCanonicalProvisioningReceipt receipt,
        ArtifactTrustLock artifactLock,
        string repositoryRoot,
        string stagingRoot,
        IFullCanonicalArtifactDatabase database,
        CancellationToken cancellationToken = default)
    {
        var artifacts = SelectArtifacts(receipt.RunKind, artifactLock);
        if (receipt.RepositoryMigration != RepositoryMigrationState.Read(repositoryRoot)
            || receipt.Artifacts.Count != artifacts.Count)
        {
            throw new InvalidOperationException("The full-canonical provisioning receipt is stale or incomplete.");
        }

        for (var index = 0; index < artifacts.Count; index++)
        {
            var artifact = artifacts[index];
            var expected = receipt.Artifacts[index];
            if (expected.Id != artifact.Id
                || expected.ImmutableStorageId != artifact.ImmutableStorageId)
            {
                throw new InvalidOperationException("The full-canonical provisioning receipt does not match the lock.");
            }

            VerifyArtifactTrust(artifactLock, artifact, repositoryRoot, stagingRoot);
            await database.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await database.AssertMigrationAsync(artifact.Migration, cancellationToken);
            var actual = await VerifyRestoredArtifactAsync(
                artifact,
                stagingRoot,
                database,
                cancellationToken);
            if (!ProvisionedArtifactsMatch(actual, expected))
            {
                throw new InvalidOperationException("The shared full-canonical database state differs from its receipt.");
            }
        }
    }

    private static IReadOnlyList<LockedArtifact> SelectArtifacts(string runKind, ArtifactTrustLock artifactLock)
    {
        if (runKind is not "scheduled" and not "release")
        {
            throw new InvalidOperationException("Full-canonical provisioning applies only to scheduled or release runs.");
        }

        var lockIssue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (lockIssue is not null)
        {
            throw new InvalidOperationException($"The artifact lock is invalid: {lockIssue}");
        }

        var artifacts = artifactLock.Artifacts
            .Where(artifact => artifact.RequiredLanes.Contains(runKind, StringComparer.Ordinal)
                && artifact.Restore?.Kind == "full-canonical")
            .OrderBy(artifact => artifact.Restore!.Order)
            .ThenBy(artifact => artifact.Id, StringComparer.Ordinal)
            .ToArray();
        if (artifacts.Length == 0)
        {
            throw new InvalidOperationException(
                $"Run '{runKind}' has no locked full-canonical artifact and cannot be provisioned.");
        }

        var duplicateOrder = artifacts
            .GroupBy(artifact => artifact.Restore!.Order)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOrder is not null)
        {
            throw new InvalidOperationException(
                $"Full-canonical restore order {duplicateOrder.Key} is assigned to more than one artifact.");
        }

        var duplicateTable = artifacts
            .SelectMany(artifact => artifact.TableScope.Tables.Select(table => (artifact.Id, Table: table)))
            .GroupBy(entry => entry.Table, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTable is not null)
        {
            throw new InvalidOperationException(
                $"Full-canonical table '{duplicateTable.Key}' is owned by more than one artifact.");
        }

        return artifacts;
    }

    private static async Task PreflightRestoreTargetAsync(
        IReadOnlyList<LockedArtifact> artifacts,
        IFullCanonicalArtifactDatabase database,
        CancellationToken cancellationToken)
    {
        foreach (var artifact in artifacts)
        {
            await database.AssertPostgreSqlCompatibilityAsync(artifact.PostgreSql, cancellationToken);
            await database.AssertMigrationAsync(artifact.Migration, cancellationToken);
        }

        await database.AssertRestoreTargetIsEmptyAsync(
            artifacts.SelectMany(artifact => artifact.TableScope.Tables).ToArray(),
            cancellationToken);
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
            throw new InvalidOperationException(
                $"Full-canonical artifact trust verification failed for '{artifact.Id}': {trust.Detail}");
        }
    }

    private static async Task<FullCanonicalProvisionedArtifact> VerifyRestoredArtifactAsync(
        LockedArtifact artifact,
        string stagingRoot,
        IFullCanonicalArtifactDatabase database,
        CancellationToken cancellationToken)
    {
        var manifest = TestArtifactManifest.ReadFrom(Path.Combine(stagingRoot, artifact.ManifestPath));
        var actualRows = await database.CountRowsAsync(
            manifest.Tables.Select(table => table.Name).ToArray(),
            cancellationToken);
        foreach (var table in manifest.Tables)
        {
            if (!actualRows.TryGetValue(table.Name, out var actual) || actual != table.Rows)
            {
                throw new InvalidOperationException(
                    $"Full-canonical restored row count mismatch for '{artifact.Id}' table "
                    + $"'{table.Name}': manifest {table.Rows}, restored {actual}.");
            }
        }

        var restore = artifact.Restore!;
        var sentinelRows = await database.CountRowsAsync(
            restore.SentinelTables.Select(sentinel => sentinel.Table).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        var sentinels = restore.SentinelTables
            .Select(sentinel =>
            {
                var actual = sentinelRows[sentinel.Table];
                if (actual != sentinel.ExpectedCount)
                {
                    throw new InvalidOperationException(
                        $"Full-canonical canonical sentinel mismatch for '{artifact.Id}' sentinel "
                        + $"'{sentinel.Id}': expected {sentinel.ExpectedCount}, restored {actual}.");
                }

                return new FullCanonicalSentinelResult(
                    sentinel.Id,
                    sentinel.Table,
                    sentinel.ExpectedCount,
                    actual);
            })
            .ToArray();

        return new FullCanonicalProvisionedArtifact(
            artifact.Id,
            artifact.ImmutableStorageId,
            manifest.Tables,
            sentinels);
    }

    private static LockedArtifactFile Payload(LockedArtifact artifact)
    {
        return artifact.StagedFiles.Single(file => file.Role == "payload");
    }

    private static bool ProvisionedArtifactsMatch(
        FullCanonicalProvisionedArtifact actual,
        FullCanonicalProvisionedArtifact expected)
    {
        return actual.Id == expected.Id
            && actual.ImmutableStorageId == expected.ImmutableStorageId
            && actual.Tables.SequenceEqual(expected.Tables)
            && actual.Sentinels.SequenceEqual(expected.Sentinels);
    }
}

internal interface IFullCanonicalArtifactFetcher
{
    Task FetchAsync(
        LockedArtifact artifact,
        string stagingRoot,
        CancellationToken cancellationToken = default);
}

internal interface IFullCanonicalArtifactDatabase
{
    Task AssertPostgreSqlCompatibilityAsync(
        LockedPostgreSqlState expected,
        CancellationToken cancellationToken = default);

    Task AssertMigrationAsync(
        ArtifactMigrationState expected,
        CancellationToken cancellationToken = default);

    Task AssertRestoreTargetIsEmptyAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        LockedArtifact artifact,
        string payloadPath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, long>> CountRowsAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default);
}

internal sealed record FullCanonicalProvisioningReceipt(
    string Status,
    string RunKind,
    ArtifactMigrationState RepositoryMigration,
    IReadOnlyList<FullCanonicalProvisionedArtifact> Artifacts);

internal sealed record FullCanonicalProvisionedArtifact(
    string Id,
    string ImmutableStorageId,
    IReadOnlyList<ArtifactManifestTable> Tables,
    IReadOnlyList<FullCanonicalSentinelResult> Sentinels);

internal sealed record FullCanonicalSentinelResult(
    string Id,
    string Table,
    long ExpectedCount,
    long ActualCount);
