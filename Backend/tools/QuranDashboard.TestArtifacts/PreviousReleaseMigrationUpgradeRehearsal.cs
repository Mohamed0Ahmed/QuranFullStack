using System.Diagnostics;
using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

// The tool owns phase ordering and evidence shape, while the test project supplies the Testcontainers and
// application-host adapters. This keeps the command read-only and makes synthetic failure tests faithful.
internal static class PreviousReleaseMigrationUpgradeRehearsal
{
    internal static async Task<PreviousReleaseMigrationUpgradeEvidence> RunAsync(
        PreviousReleaseMigrationUpgradePlan plan,
        ArtifactTrustLock artifactLock,
        string repositoryRoot,
        string stagingRoot,
        IFullCanonicalArtifactFetcher fetcher,
        IPreviousReleaseMigrationUpgradeDatabase database,
        Func<CancellationToken, Task> bootApplication,
        Func<CancellationToken, Task> verifyCriticalReads,
        Action<PreviousReleaseMigrationUpgradeEvidence>? retainEvidence = null,
        CancellationToken cancellationToken = default)
    {
        var evidence = new PreviousReleaseMigrationUpgradeEvidence(
            "running",
            new PreviousReleaseMigrationEvidence(
                plan.AuthoritativePreviousRelease.Sha,
                plan.Expectations.AuthoritativeForwardMigrationIds),
            new PreviousReleaseMigrationEvidence(
                plan.SupplementalRehearsalBaseline.Sha,
                plan.Expectations.SupplementalForwardMigrationIds),
            plan.Artifact.PayloadSha256,
            plan.Artifact.ManifestSha256,
            new PreviousReleaseTableSentinelEvidence(
                plan.Expectations.PreUpgradeSentinel!.Table,
                plan.Expectations.PreUpgradeSentinel.ExpectedCount,
                null),
            new PreviousReleaseTableSentinelEvidence(
                plan.Expectations.PostUpgradeSentinel!.Table,
                plan.Expectations.PostUpgradeSentinel.ExpectedCount,
                null),
            new PreviousReleasePhraseSearchEvidence(
                plan.Expectations.PhraseSearch!.StateTable,
                plan.Expectations.PhraseSearch.ExpectedRows,
                plan.Expectations.PhraseSearch.ActiveBuild,
                null,
                null),
            new PreviousReleaseCheckEvidence("succeeded", "not-run"),
            new PreviousReleaseCheckEvidence("succeeded", "not-run"),
            []);
        try
        {
            await RunPhaseAsync("artifact", async () =>
            {
                Directory.CreateDirectory(stagingRoot);
                var lockedArtifact = artifactLock.Artifacts.Single(artifact => artifact.Id == plan.Artifact.Id);
                await fetcher.FetchAsync(lockedArtifact, stagingRoot, cancellationToken);
                var trust = ArtifactTrustVerifier.Verify(artifactLock, lockedArtifact, repositoryRoot, stagingRoot);
                if (trust.State != ArtifactTrustState.Present)
                {
                    throw new InvalidOperationException("artifact trust mismatch");
                }
            }, evidence, cancellationToken);
            await RunPhaseAsync("historical-schema", async () =>
            {
                await database.MigrateToAsync(plan.SupplementalRehearsalBaseline.Migration!.Head, cancellationToken);
                await AssertMigrationsAsync(database, plan.SupplementalRehearsalBaseline.Migration.Inventory, cancellationToken);
            }, evidence, cancellationToken);
            await RunPhaseAsync("restore", async () =>
            {
                var lockedArtifact = artifactLock.Artifacts.Single(artifact => artifact.Id == plan.Artifact.Id);
                var payload = lockedArtifact.StagedFiles.Single(file => file.Role == "payload");
                await database.RestoreAsync(
                    Path.Combine(stagingRoot, payload.Path),
                    plan.Artifact.TableScope!,
                    cancellationToken);
                foreach (var table in plan.Artifact.TableCounts!)
                {
                    await AssertCountAsync(
                        database,
                        new PreviousReleaseTableSentinel(table.Name, table.Rows),
                        cancellationToken);
                }
                var actual = await AssertCountAsync(database, plan.Expectations.PreUpgradeSentinel!, cancellationToken);
                evidence = evidence with
                {
                    PreUpgradeCanonicalSentinel = new PreviousReleaseTableSentinelEvidence(
                        plan.Expectations.PreUpgradeSentinel.Table,
                        plan.Expectations.PreUpgradeSentinel.ExpectedCount,
                        actual),
                };
            }, evidence, cancellationToken);
            await RunPhaseAsync("forward-migrations", async () =>
            {
                await database.MigrateToAsync(plan.Artifact.Migration!.Head, cancellationToken);
                await AssertMigrationsAsync(database, plan.Artifact.Migration.Inventory, cancellationToken);
            }, evidence, cancellationToken);
            await RunPhaseAsync("application-boot", async () =>
            {
                await bootApplication(cancellationToken);
                evidence = evidence with { ApplicationBoot = new PreviousReleaseCheckEvidence("succeeded", "succeeded") };
            }, evidence, cancellationToken);
            await RunPhaseAsync("critical-read-sentinels", async () =>
            {
                await verifyCriticalReads(cancellationToken);
                evidence = evidence with { CriticalReadSentinels = new PreviousReleaseCheckEvidence("succeeded", "succeeded") };
            }, evidence, cancellationToken);
            await RunPhaseAsync("post-upgrade-sentinels", async () =>
            {
                var canonicalActual = await AssertCountAsync(database, plan.Expectations.PostUpgradeSentinel!, cancellationToken);
                evidence = evidence with
                {
                    PostUpgradeCanonicalSentinel = new PreviousReleaseTableSentinelEvidence(
                        plan.Expectations.PostUpgradeSentinel.Table,
                        plan.Expectations.PostUpgradeSentinel.ExpectedCount,
                        canonicalActual),
                };
                var phraseSearchActual = await database.PhraseSearchStateAsync(plan.Expectations.PhraseSearch!, cancellationToken);
                if (phraseSearchActual.Rows != plan.Expectations.PhraseSearch.ExpectedRows || phraseSearchActual.ActiveBuildRows != 0)
                {
                    throw new InvalidOperationException("phrase search state mismatch");
                }
                evidence = evidence with
                {
                    PhraseSearch = new PreviousReleasePhraseSearchEvidence(
                        plan.Expectations.PhraseSearch.StateTable,
                        plan.Expectations.PhraseSearch.ExpectedRows,
                        plan.Expectations.PhraseSearch.ActiveBuild,
                        phraseSearchActual.Rows,
                        phraseSearchActual.ActiveBuildRows),
                };
            }, evidence, cancellationToken);
            evidence = evidence with { Status = "passed" };
            retainEvidence?.Invoke(evidence);
            return evidence;
        }
        catch (PreviousReleaseMigrationUpgradeRehearsalException)
        {
            evidence = evidence with { Status = "failed" };
            retainEvidence?.Invoke(evidence);
            throw;
        }
    }

    private static async Task RunPhaseAsync(
        string phase,
        Func<Task> action,
        PreviousReleaseMigrationUpgradeEvidence evidence,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action();
            evidence.Phases.Add(new PreviousReleaseMigrationUpgradePhase(phase, "passed", stopwatch.ElapsedMilliseconds, "completed"));
        }
        catch (PreviousReleaseMigrationUpgradePhaseFailureException exception)
        {
            evidence.Phases.Add(new PreviousReleaseMigrationUpgradePhase(phase, "failed", stopwatch.ElapsedMilliseconds, exception.Detail));
            throw new PreviousReleaseMigrationUpgradeRehearsalException(phase);
        }
        catch (Exception exception)
        {
            evidence.Phases.Add(new PreviousReleaseMigrationUpgradePhase(phase, "failed", stopwatch.ElapsedMilliseconds, $"unexpected-{exception.GetType().Name}"));
            throw new PreviousReleaseMigrationUpgradeRehearsalException(phase);
        }
    }

    private static async Task AssertMigrationsAsync(
        IPreviousReleaseMigrationUpgradeDatabase database,
        IReadOnlyList<string> expected,
        CancellationToken cancellationToken)
    {
        var applied = await database.AppliedMigrationsAsync(cancellationToken);
        if (!applied.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("migration inventory mismatch");
        }
    }

    private static async Task<long> AssertCountAsync(
        IPreviousReleaseMigrationUpgradeDatabase database,
        PreviousReleaseTableSentinel sentinel,
        CancellationToken cancellationToken)
    {
        var actual = await database.CountRowsAsync(sentinel.Table, cancellationToken);
        if (actual != sentinel.ExpectedCount)
        {
            throw new InvalidOperationException("sentinel count mismatch");
        }

        return actual;
    }
}

internal interface IPreviousReleaseMigrationUpgradeDatabase
{
    Task MigrateToAsync(string migrationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> AppliedMigrationsAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(string payloadPath, IReadOnlyList<string> tables, CancellationToken cancellationToken = default);
    Task<long> CountRowsAsync(string table, CancellationToken cancellationToken = default);
    Task<PreviousReleasePhraseSearchActual> PhraseSearchStateAsync(PreviousReleasePhraseSearchExpectation expectation, CancellationToken cancellationToken = default);
}

internal sealed record PreviousReleaseMigrationUpgradeEvidence(
    string Status,
    PreviousReleaseMigrationEvidence AuthoritativePreviousRelease,
    PreviousReleaseMigrationEvidence SupplementalRehearsalBaseline,
    string PayloadSha256,
    string ManifestSha256,
    PreviousReleaseTableSentinelEvidence PreUpgradeCanonicalSentinel,
    PreviousReleaseTableSentinelEvidence PostUpgradeCanonicalSentinel,
    PreviousReleasePhraseSearchEvidence PhraseSearch,
    PreviousReleaseCheckEvidence ApplicationBoot,
    PreviousReleaseCheckEvidence CriticalReadSentinels,
    List<PreviousReleaseMigrationUpgradePhase> Phases)
{
    internal string ToSanitizedJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions(JsonSerializerDefaults.Web));
}

internal sealed record PreviousReleaseMigrationEvidence(string Commit, IReadOnlyList<string> ForwardMigrationIds);
internal sealed record PreviousReleaseTableSentinelEvidence(string Table, long ExpectedRows, long? ActualRows);
internal sealed record PreviousReleasePhraseSearchEvidence(
    string StateTable,
    long ExpectedRows,
    string ExpectedActiveBuild,
    long? ActualRows,
    long? ActualActiveBuildRows);
internal sealed record PreviousReleaseCheckEvidence(string Expected, string Actual);
internal sealed record PreviousReleasePhraseSearchActual(long Rows, long ActiveBuildRows);
internal sealed record PreviousReleaseMigrationUpgradePhase(string Name, string Status, long DurationMilliseconds, string Detail);
internal sealed record PreviousReleaseMigrationUpgradeSetupFailureEvidence(
    string Status,
    string Phase,
    string Detail,
    string MigrationState)
{
    internal static PreviousReleaseMigrationUpgradeSetupFailureEvidence Create(string phase, Exception exception) =>
        new("failed", phase, $"unexpected-{exception.GetType().Name}", "not-started");

    internal string ToSanitizedJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
internal sealed class PreviousReleaseMigrationUpgradeRehearsalException(string phase) : InvalidOperationException($"Previous-release migration rehearsal failed at phase={phase}.");
internal sealed class PreviousReleaseMigrationUpgradePhaseFailureException(string detail) : InvalidOperationException
{
    internal string Detail { get; } = detail;
}
