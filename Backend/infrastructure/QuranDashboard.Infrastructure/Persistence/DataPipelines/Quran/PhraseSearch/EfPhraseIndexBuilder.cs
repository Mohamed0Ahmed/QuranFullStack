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

    public EfPhraseIndexBuilder(
        QuranDashboardDbContext dbContext,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseIndexBuildDatabase database,
        PhraseIndexExactStager exactStager,
        PhraseSimilarityBuilder similarityBuilder,
        PhraseIndexValidator validator,
        PhraseIndexActivator activator,
        PhraseIndexBuildFinalizer finalizer)
    {
        this.dbContext = dbContext;
        this.sourceStateCoordinator = sourceStateCoordinator;
        this.database = database;
        this.exactStager = exactStager;
        this.similarityBuilder = similarityBuilder;
        this.validator = validator;
        this.activator = activator;
        this.finalizer = finalizer;
    }

    public async Task<PhraseIndexBuildExecution> BuildAsync(
        bool force,
        string reportRootDirectory,
        CancellationToken ct)
    {
        var run = new PhraseIndexBuildRun(Guid.NewGuid(), force, reportRootDirectory);
        Directory.CreateDirectory(run.ReportDirectory);
        NpgsqlConnection? connection = null;
        var activated = false;

        try
        {
            connection = await OpenConnectionAsync(ct);
            var bootstrap = await sourceStateCoordinator.BootstrapAsync(connection, ct);
            run.SourceRevision = bootstrap.State.SourceRevision;
            run.SourceFingerprint = bootstrap.ComputedFingerprint;
            run.Checks.AddRange(bootstrap.Source.Checks);
            if (!bootstrap.Source.Passed)
            {
                return await finalizer.FinishFailureAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Failed,
                    "Phrase index source integrity checks failed.",
                    "Failed",
                    buildPersisted: false,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
            }

            await database.AcquireBuilderLockAsync(connection, ct);
            run.BuilderLockHeld = true;
            var snapshot = await database.ReadSourceSnapshotAsync(connection, ct);
            run.SourceRevision = snapshot.SourceRevision;
            run.SourceFingerprint = snapshot.SourceFingerprint;
            run.ActiveBuildId = snapshot.ActiveBuildId;
            run.PreviousBuildId = snapshot.PreviousBuildId;
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
                    "Failed",
                    buildPersisted: false,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
            }

            if (!force && await database.HasActiveBuildAsync(connection, ct))
            {
                return await finalizer.FinishFailureAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Refused,
                    "An active phrase index already exists. Re-run with --force to build a replacement.",
                    "Refused",
                    buildPersisted: false,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
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

            await database.CreateBuildAsync(connection, run.BuildId, snapshot, run.StartedAtUtc, ct);
            run.BuildPersisted = true;

            if (!run.DiskPreflight.Passed)
            {
                await database.MarkFailedAsync(
                    connection,
                    run.BuildId,
                    "fail",
                    "disk-preflight-failed",
                    ct);
                return await finalizer.FinishFailureAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Failed,
                    "Phrase index disk preflight failed.",
                    "Failed",
                    buildPersisted: true,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
            }

            if (!string.Equals(
                    PhraseIndexBuildConstants.ApprovedSourceFingerprint,
                    snapshot.SourceFingerprint,
                    StringComparison.Ordinal))
            {
                AddOrReplaceCheck(run.Checks, new PhraseBuildCheck(
                    "SOURCE-APPROVAL",
                    "hard",
                    string.IsNullOrEmpty(PhraseIndexBuildConstants.ApprovedSourceFingerprint)
                        ? "approved fingerprint"
                        : PhraseIndexBuildConstants.ApprovedSourceFingerprint,
                    snapshot.SourceFingerprint,
                    false));
                await database.MarkFailedAsync(
                    connection,
                    run.BuildId,
                    "source-approval-required",
                    "source-approval-required",
                    ct);
                return await finalizer.FinishFailureAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.SourceApprovalRequired,
                    "Phrase index source fingerprint requires approval.",
                    "SourceApprovalRequired",
                    buildPersisted: true,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
            }

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
                    "Failed",
                    buildPersisted: true,
                    persistedGeneration: false,
                    exactReady: false,
                    similarityReady: false);
            }

            var activation = await activator.ActivateAsync(
                connection,
                run.BuildId,
                snapshot.SourceRevision,
                snapshot.SourceFingerprint,
                ct);
            run.SourceRevisionAtActivation = activation.SourceRevisionAtActivation;
            run.SourceFingerprintAtActivation = activation.SourceFingerprintAtActivation;
            run.PreviousBuildId = activation.PreviousBuildId;
            run.ActiveBuildId = activation.ActiveBuildId;

            if (!activation.Activated)
            {
                run.Errors.Add(activation.FailureReason);
                return await finalizer.FinishFailureAsync(
                    connection,
                    run,
                    PhraseIndexBuildOutcome.Failed,
                    "Phrase index activation was rejected by the source fence.",
                    "Failed",
                    buildPersisted: true,
                    persistedGeneration: true,
                    exactReady: true,
                    similarityReady: true);
            }

            activated = true;
        }
        catch (OperationCanceledException)
        {
            if (run.BuildPersisted && connection is not null)
            {
                await database.MarkFailedAsync(
                    connection,
                    run.BuildId,
                    "cancelled",
                    "build-cancelled",
                    CancellationToken.None);
            }

            run.Errors.Add("Build cancelled before activation.");
            return await finalizer.FinishFailureAsync(
                connection,
                run,
                PhraseIndexBuildOutcome.Cancelled,
                "Phrase index build was cancelled; the prior active generation was retained.",
                "Cancelled",
                run.BuildPersisted,
                persistedGeneration: false,
                exactReady: false,
                similarityReady: false);
        }
        catch (Exception ex)
        {
            var failureDiagnostic = BuildFailureDiagnostic(ex);
            if (run.BuildPersisted && connection is not null)
            {
                await database.MarkFailedAsync(
                    connection,
                    run.BuildId,
                    "fail",
                    failureDiagnostic,
                    CancellationToken.None);
            }

            run.Errors.Add(failureDiagnostic);
            return await finalizer.FinishFailureAsync(
                connection,
                run,
                PhraseIndexBuildOutcome.Failed,
                "Phrase index build failed. See the redacted report.",
                "Failed",
                run.BuildPersisted,
                persistedGeneration: false,
                exactReady: false,
                similarityReady: false);
        }
        finally
        {
            if (run.BuilderLockHeld && connection is not null)
            {
                await database.ReleaseBuilderLockAsync(connection);
            }
        }

        if (!activated || connection is null)
        {
            throw new InvalidOperationException("Phrase index activation did not produce a finalizable build.");
        }

        return await finalizer.FinishActivatedAsync(connection, run);
    }

    private async Task<PhraseIndexValidationResult> StageAndValidateAsync(
        NpgsqlConnection connection,
        PhraseSourceSnapshot snapshot,
        PhraseIndexBuildRun run,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var exact = await exactStager.StageAsync(
            connection,
            transaction,
            run.BuildId,
            snapshot.Tokens,
            snapshot.MaximumAyahLength,
            ct);
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

    private static string BuildFailureDiagnostic(Exception exception) => exception switch
    {
        InvalidOperationException { InnerException: PostgresException postgresException } =>
            $"{exception.Message} PostgreSQL {postgresException.SqlState}; position={postgresException.Position}.",
        PostgresException postgresException =>
            $"PostgreSQL {postgresException.SqlState}; position={postgresException.Position}; "
            + $"constraint={postgresException.ConstraintName ?? "none"}",
        InvalidOperationException => $"InvalidOperationException: {exception.Message}",
        InvalidCastException => "InvalidCastException: database value type mismatch",
        OverflowException => "OverflowException: numeric value exceeded its contract",
        _ => $"{exception.GetType().Name}: build-failed",
    };

}
