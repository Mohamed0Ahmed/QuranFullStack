using System.Diagnostics;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class EfPhraseIndexBuilder : IPhraseIndexBuilder
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly PhraseSourceStateCoordinator sourceStateCoordinator;
    private readonly PhraseIndexBuildDatabase database;
    private readonly PhraseIndexExactStager exactStager;
    private readonly PhraseSimilarityBuilder similarityBuilder;
    private readonly PhraseIndexValidator validator;
    private readonly PhraseIndexActivator activator;
    private readonly PhraseIndexBuildFinalizer finalizer;
    private readonly PhraseIndexPreActivationFailureFinalizer failureFinalizer;
    private readonly PhraseIndexBuildExpectations expectations;
    private readonly PhraseIndexBuildLifecycleTestHook testHook;

    public EfPhraseIndexBuilder(
        QuranDashboardDbContext dbContext,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseIndexBuildDatabase database,
        PhraseIndexExactStager exactStager,
        PhraseSimilarityBuilder similarityBuilder,
        PhraseIndexValidator validator,
        PhraseIndexActivator activator,
        PhraseIndexBuildFinalizer finalizer,
        PhraseIndexPreActivationFailureFinalizer failureFinalizer,
        PhraseIndexBuildExpectations expectations,
        PhraseIndexBuildLifecycleTestHook testHook)
    {
        this.dbContext = dbContext;
        this.sourceStateCoordinator = sourceStateCoordinator;
        this.database = database;
        this.exactStager = exactStager;
        this.similarityBuilder = similarityBuilder;
        this.validator = validator;
        this.activator = activator;
        this.finalizer = finalizer;
        this.failureFinalizer = failureFinalizer;
        this.expectations = expectations;
        this.testHook = testHook;
    }

    public async Task<PhraseIndexBuildExecution> BuildAsync(
        string reportRootDirectory,
        CancellationToken ct)
    {
        var run = new PhraseIndexBuildRun(Guid.NewGuid(), reportRootDirectory);
        Directory.CreateDirectory(run.ReportDirectory);
        NpgsqlConnection? connection = null;
        var activated = false;
        var activationOutcomeUnknown = false;

        try
        {
            try
            {
                connection = await OpenConnectionAsync(ct);
                run.CurrentStage = PhraseIndexBuildStage.PrepareBuild;
                await database.AcquireBuilderLockAsync(connection, ct);
                run.BuilderLockHeld = true;
                var recoveredBuildCount = await database.RecoverAbandonedBuildsAsync(connection, ct);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "ABANDONED-BUILD-RECOVERY",
                    "hard",
                    "0 unresolved unreferenced building or validated builds",
                    $"0 unresolved; {recoveredBuildCount.ToString(CultureInfo.InvariantCulture)} recovered",
                    true));
                if (recoveredBuildCount > 0)
                {
                    run.Warnings.Add(
                        $"Recovered {recoveredBuildCount.ToString(CultureInfo.InvariantCulture)} "
                        + "abandoned phrase index build(s) before checking one-shot eligibility.");
                }

                var pendingGenerationCleanup = await sourceStateCoordinator.CleanupUnreferencedGenerationsAsync(
                    connection,
                    CancellationToken.None);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "UNREFERENCED-GENERATION-CLEANUP",
                    "hard",
                    "no unreferenced generation data",
                    pendingGenerationCleanup.Succeeded ? "verified" : "cleanup pending",
                    pendingGenerationCleanup.Succeeded));
                if (!pendingGenerationCleanup.Succeeded)
                {
                    run.Warnings.Add(
                        pendingGenerationCleanup.Warning ?? PhraseIndexGenerationCleanup.PendingWarning);
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index generation cleanup remains pending; no new build was started. Retry the command.",
                        "Failed");
                }

                var hasDataGeneration = await database.HasDataGenerationAsync(connection, ct);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "ONE-SHOT-EMPTY-DATABASE",
                    "hard",
                    "no active, previous, non-failed, or data-bearing PhraseSearch generation",
                    hasDataGeneration ? "existing generation" : "empty",
                    !hasDataGeneration));
                if (hasDataGeneration)
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Refused,
                        "Phrase index build refused: an active or data-bearing generation already exists. Full database reset is required before another build; metadata-only failed audits do not block retry.",
                        "Refused");
                }

                run.DiskPreflight = await database.ReadDiskPreflightAsync(
                    connection,
                    ct);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "DISK-STORAGE-PROOF",
                    "hard",
                    "verified database filesystem free bytes",
                    run.DiskPreflight.ProofKind,
                    run.DiskPreflight.ProofVerified));
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "DISK-PREFLIGHT",
                    "hard",
                    $">={run.DiskPreflight.RequiredFreeBytes.ToString(CultureInfo.InvariantCulture)}",
                    run.DiskPreflight.AvailableDatabaseFilesystemBytes.ToString(CultureInfo.InvariantCulture),
                    run.DiskPreflight.Passed));
                if (!run.DiskPreflight.Passed)
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index disk preflight failed.",
                        "Failed");
                }

                run.CurrentStage = PhraseIndexBuildStage.BootstrapSource;
                var bootstrap = await sourceStateCoordinator.BootstrapAsync(
                    connection,
                    expectations.ApprovedSourceFingerprint,
                    expectations.ApprovedSourceFingerprintVersion,
                    ct);
                run.SourceRevision = bootstrap.State.SourceRevision;
                run.SourceFingerprint = bootstrap.ComputedFingerprint;
                run.Checks.AddRange(bootstrap.Source.Checks);
                if (bootstrap.CleanupWarning is not null)
                {
                    run.Warnings.Add(bootstrap.CleanupWarning);
                }

                if (!bootstrap.Source.Passed)
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index source integrity checks failed.",
                        "Failed");
                }

                var sourceApproved =
                    expectations.ApprovedSourceFingerprintVersion
                        == PhraseIndexBuildConstants.SourceFingerprintVersion
                    && string.Equals(
                        expectations.ApprovedSourceFingerprint,
                        bootstrap.ComputedFingerprint,
                        StringComparison.Ordinal);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "SOURCE-APPROVAL",
                    "hard",
                    $"v{expectations.ApprovedSourceFingerprintVersion.ToString(CultureInfo.InvariantCulture)}:{expectations.ApprovedSourceFingerprint}",
                    $"v{PhraseIndexBuildConstants.SourceFingerprintVersion.ToString(CultureInfo.InvariantCulture)}:{bootstrap.ComputedFingerprint}",
                    sourceApproved));
                if (!sourceApproved)
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.SourceApprovalRequired,
                        "Phrase index source fingerprint requires approval.",
                        "SourceApprovalRequired");
                }

                if (bootstrap.CleanupWarning is not null)
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index cleanup remains pending after source-state work was committed; no new build was started. Retry the command.",
                        "Failed");
                }

                var snapshot = await database.ReadSourceSnapshotAsync(connection, ct);
                run.SourceRevision = snapshot.SourceRevision;
                run.SourceFingerprint = snapshot.SourceFingerprint;
                run.ActiveBuildId = snapshot.ActiveBuildId;
                MergeChecks(run.Checks, snapshot.Checks);
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "SOURCE-STATE-FINGERPRINT",
                    "hard",
                    snapshot.StoredSourceFingerprint,
                    snapshot.SourceFingerprint,
                    string.Equals(
                        snapshot.StoredSourceFingerprint,
                        snapshot.SourceFingerprint,
                        StringComparison.Ordinal)));
                if (run.Checks.Any(check => check.Severity == "hard" && !check.Passed))
                {
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index source snapshot is not compatible with source state.",
                        "Failed");
                }

                await database.CreateBuildAsync(connection, run.BuildId, snapshot, run.StartedAtUtc, ct);
                run.BuildPersisted = true;

                var staging = await StageAndValidateAsync(connection, snapshot, run, ct);
                if (!staging.Passed)
                {
                    await database.MarkFailedAsync(
                        connection,
                        run.BuildId,
                        "fail",
                        "hard-check-failed",
                        ct);
                    return await finalizer.FinishFailureAsync(
                        connection,
                        run,
                        PhraseIndexBuildOutcome.Failed,
                        "Phrase index hard checks failed.",
                        "Failed");
                }

                run.CurrentStage = PhraseIndexBuildStage.ActivateBuild;
                var activation = await activator.ActivateAsync(
                    connection,
                    run.BuildId,
                    snapshot.SourceRevision,
                    snapshot.SourceFingerprint,
                    ct);
                if (!activation.OutcomeKnown)
                {
                    activationOutcomeUnknown = true;
                    run.Errors.Add("activation-outcome-unresolved");
                    run.Warnings.Add(
                        "Activation outcome could not be reconciled; the build was not marked failed or deleted.");
                }
                else
                {
                    run.SourceRevisionAtActivation = activation.SourceRevisionAtActivation;
                    run.SourceFingerprintAtActivation = activation.SourceFingerprintAtActivation;
                    run.ActiveBuildId = activation.ActiveBuildId;

                    if (!activation.Activated)
                    {
                        run.Errors.Add(activation.FailureReason);
                        return await finalizer.FinishFailureAsync(
                            connection,
                            run,
                            PhraseIndexBuildOutcome.Failed,
                            "Phrase index activation was rejected by the source fence.",
                            "Failed");
                    }

                    AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                        "POST-ACTIVATION-SINGLE-GENERATION",
                        "hard",
                        "one active data generation, no previous build, metadata-only failed audits allowed",
                        "verified",
                        true));

                    if (activation.ReconciledAfterFailure)
                    {
                        run.RecordActivationFinalizationFailure(
                            "activation-acknowledgement-failed",
                            "The database confirms this build is active after the activation acknowledgement failed.");
                    }

                    activated = true;
                }
            }
            catch (OperationCanceledException)
            {
                run.Errors.Add("Build cancelled before activation.");
                return await failureFinalizer.FinishAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Cancelled,
                    "Phrase index build was cancelled; attempt data was removed and no new generation was activated.",
                    "Cancelled",
                    "cancelled",
                    "build-cancelled");
            }
            catch (Exception ex)
            {
                var failureDiagnostic = run.BuildFailureDiagnostic(ex);
                run.Errors.Add(failureDiagnostic);
                return await failureFinalizer.FinishAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Failed,
                    "Phrase index build failed.",
                    "Failed",
                    "fail",
                    failureDiagnostic);
            }

            if (activationOutcomeUnknown)
            {
                if (connection is null)
                {
                    throw new InvalidOperationException(
                        "Phrase index activation outcome is unknown and no connection remains for finalization.");
                }

                return await finalizer.FinishActivationOutcomeUnknownAsync(connection, run);
            }

            if (!activated || connection is null)
            {
                throw new InvalidOperationException("Phrase index activation did not produce a finalizable build.");
            }

            return await finalizer.FinishActivatedAsync(connection, run);
        }
        finally
        {
            await PhraseIndexBuilderLockRelease.ReleaseAsync(
                database,
                connection,
                run);
        }
    }

    private async Task<PhraseIndexValidationResult> StageAndValidateAsync(
        NpgsqlConnection connection,
        PhraseSourceSnapshot snapshot,
        PhraseIndexBuildRun run,
        CancellationToken ct)
    {
        run.CurrentStage = PhraseIndexBuildStage.StageExactIndex;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var exact = await exactStager.StageAsync(
            connection,
            transaction,
            run.BuildId,
            snapshot.Tokens,
            snapshot.MaximumAyahLength,
            ct);
        run.CurrentStage = PhraseIndexBuildStage.BuildSimilarityIndex;
        var similarity = await similarityBuilder.BuildAsync(
            connection,
            transaction,
            run.BuildId,
            exact.Metrics,
            ct);
        run.Metrics = exact.Metrics
            .Where(metric => metric.WordCount < PhraseIndexBuildConstants.MinimumSimilarityLength)
            .Concat(similarity.Metrics)
            .ToList();
        run.Totals = exact.Totals with
        {
            SimilarityEdges = similarity.EdgeCount,
            SimilarityAnchorStats = similarity.AnchorStatCount,
        };

        run.CurrentStage = PhraseIndexBuildStage.ValidateStagedIndex;
        await testHook.AfterStagingAsync(run.BuildId, ct);
        var validation = await validator.ValidateAsync(
            connection,
            transaction,
            run.BuildId,
            run.Checks,
            ct);
        run.Totals = validation.Totals;
        run.Checks = validation.Checks.ToList();
        if (!validation.Passed)
        {
            await transaction.RollbackAsync(ct);
            return validation;
        }

        run.CurrentStage = PhraseIndexBuildStage.PersistStagedIndex;
        await database.MarkValidatedAsync(
            connection,
            transaction,
            run.BuildId,
            validation.Totals,
            ct);
        await transaction.CommitAsync(ct);
        return validation;
    }

    private static void MergeChecks(
        ICollection<PhraseBuildCheck> target,
        IEnumerable<PhraseBuildCheck> source)
    {
        foreach (var check in source)
        {
            AddOrReplaceCheck(target, check);
        }
    }

    private static void AddOrReplaceCheck(
        ICollection<PhraseBuildCheck> checks,
        PhraseBuildCheck check)
    {
        var existing = checks.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, check.Id, StringComparison.Ordinal));
        if (existing is not null)
        {
            checks.Remove(existing);
        }

        checks.Add(check);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for phrase index build.");
        }

        if (npgsqlConnection.State != ConnectionState.Open)
        {
            await npgsqlConnection.OpenAsync(ct);
        }

        return npgsqlConnection;
    }
}
