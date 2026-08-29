using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexBuildFinalizer
{
    private readonly PhraseIndexBuildDatabase database;
    private readonly PhraseIndexBuildReportWriter reportWriter;

    public PhraseIndexBuildFinalizer(
        PhraseIndexBuildDatabase database,
        PhraseIndexBuildReportWriter reportWriter)
    {
        this.database = database;
        this.reportWriter = reportWriter;
    }

    internal async Task<PhraseIndexBuildExecution> FinishFailureAsync(
        NpgsqlConnection? connection,
        PhraseIndexBuildRun run,
        PhraseIndexBuildOutcome outcome,
        string message,
        string status)
    {
        var generationState = await ResolveFailureGenerationStateAsync(connection, run);
        if (run.BuildPersisted && connection is not null)
        {
            try
            {
                await database.CleanupExpiredFailedBuildAuditsAsync(
                    connection,
                    CancellationToken.None);
            }
            catch (Exception)
            {
                AddError(run, "failure-audit-cleanup-failed");
            }
        }

        var reportPublished = await TryWriteReportAsync(
            CreateReport(
                run,
                outcome,
                status,
                generationState.Persisted,
                active: false,
                generationState.ExactReady,
                generationState.SimilarityReady),
            run.ReportDirectory);
        if (!reportPublished)
        {
            AddError(run, "failure-report-publication-failed");
            reportPublished = await TryWriteReportAsync(
                CreateReport(
                    run,
                    outcome,
                    status,
                    generationState.Persisted,
                    active: false,
                    generationState.ExactReady,
                    generationState.SimilarityReady),
                run.ReportDirectory);
        }

        var reportLinked = false;
        if (run.BuildPersisted && connection is not null && reportPublished)
        {
            try
            {
                await database.RecordReportPathAsync(
                    connection,
                    run.BuildId,
                    run.ReportDirectory,
                    CancellationToken.None);
                reportLinked = true;
            }
            catch (Exception)
            {
                AddError(run, "failure-report-path-recording-failed");
                await TryWriteReportAsync(
                    CreateReport(
                        run,
                        outcome,
                        status,
                        generationState.Persisted,
                        active: false,
                        generationState.ExactReady,
                        generationState.SimilarityReady),
                    run.ReportDirectory);
            }
        }

        var finalMessage = reportPublished
            ? message
            : $"{message} The failure report could not be published.";
        return CreateExecution(run, outcome, finalMessage, reportPublished, reportLinked);
    }

    private async Task<PhraseIndexGenerationState> ResolveFailureGenerationStateAsync(
        NpgsqlConnection? connection,
        PhraseIndexBuildRun run)
    {
        if (!run.BuildPersisted)
        {
            return PhraseIndexGenerationState.NotPersisted;
        }

        if (connection is null)
        {
            AddError(run, "failure-generation-cleanup-unresolved");
            return PhraseIndexGenerationState.Unknown;
        }

        try
        {
            var state = await database.CleanupFailedGenerationAsync(
                connection,
                run.BuildId,
                CancellationToken.None);
            if (!state.IsAbsentAndNotReady)
            {
                AddError(run, "failure-generation-cleanup-incomplete");
            }

            return state;
        }
        catch (Exception)
        {
            AddError(run, "failure-generation-cleanup-failed");
            return PhraseIndexGenerationState.Unknown;
        }
    }

    internal async Task<PhraseIndexBuildExecution> FinishActivatedAsync(
        NpgsqlConnection connection,
        PhraseIndexBuildRun run)
    {
        try
        {
            await database.CleanupExpiredFailedBuildAuditsAsync(
                connection,
                CancellationToken.None);
        }
        catch (Exception)
        {
            run.RecordActivationFinalizationFailure(
                "post-activation-failure-audit-cleanup-failed",
                "The build is active, but expired failed-build audits could not be cleaned up.");
        }

        var outcome = run.ActivationFinalizationFailed
            ? PhraseIndexBuildOutcome.ActivatedWithFinalizationFailure
            : PhraseIndexBuildOutcome.Succeeded;
        var reportPublished = await TryWriteReportAsync(
            CreateReport(
                run,
                outcome,
                run.ActivationFinalizationFailed ? "ActiveWithFinalizationFailure" : "Active",
                persistedGeneration: true,
                active: true,
                exactReady: true,
                similarityReady: true),
            run.ReportDirectory);
        if (!reportPublished)
        {
            run.RecordActivationFinalizationFailure(
                "post-activation-report-publication-failed",
                "The build is active, but its audit report could not be published.");
            outcome = PhraseIndexBuildOutcome.ActivatedWithFinalizationFailure;
            reportPublished = await TryWriteReportAsync(
                CreateReport(
                    run,
                    outcome,
                    "ActiveWithFinalizationFailure",
                    persistedGeneration: true,
                    active: true,
                    exactReady: true,
                    similarityReady: true),
                run.ReportDirectory);
        }

        var reportLinked = false;
        if (reportPublished)
        {
            try
            {
                await database.RecordReportPathAsync(
                    connection,
                    run.BuildId,
                    run.ReportDirectory,
                    CancellationToken.None);
                reportLinked = true;
            }
            catch (Exception)
            {
                run.RecordActivationFinalizationFailure(
                    "post-activation-report-path-recording-failed",
                    "The build is active, but its report directory could not be recorded in the database.");
                outcome = PhraseIndexBuildOutcome.ActivatedWithFinalizationFailure;
                await TryWriteReportAsync(
                    CreateReport(
                        run,
                        outcome,
                        "ActiveWithFinalizationFailure",
                        persistedGeneration: true,
                        active: true,
                        exactReady: true,
                        similarityReady: true),
                    run.ReportDirectory);
            }
        }

        var message = outcome == PhraseIndexBuildOutcome.Succeeded
            ? "Phrase index build activated successfully; finalization completed."
            : "Phrase index build is active, but finalization failed; outcome=activated-with-finalization-failure.";
        return CreateExecution(run, outcome, message, reportPublished, reportLinked);
    }

    internal async Task<PhraseIndexBuildExecution> FinishActivationOutcomeUnknownAsync(
        NpgsqlConnection connection,
        PhraseIndexBuildRun run)
    {
        var reportPublished = await TryPublishActivationOutcomeUnknownReportAsync(run);
        var reportLinked = false;
        if (reportPublished)
        {
            try
            {
                await database.RecordReportPathAsync(
                    connection,
                    run.BuildId,
                    run.ReportDirectory,
                    CancellationToken.None);
                reportLinked = true;
            }
            catch (Exception)
            {
                AddError(run, "activation-outcome-report-path-recording-failed");
                reportPublished = await TryPublishActivationOutcomeUnknownReportAsync(run);
            }
        }

        var message = $"Phrase index activation outcome is unknown for build {run.BuildId}; do not rerun until active state is reconciled.";
        if (!reportPublished)
        {
            message += " The final reconciliation report is unavailable and its path is not linked from the build record.";
        }
        else if (!reportLinked)
        {
            message += " The reconciliation report is available on disk, but its path is not linked from the build record.";
        }

        return CreateExecution(
            run,
            PhraseIndexBuildOutcome.ActivationOutcomeUnknown,
            message,
            reportPublished,
            reportLinked);
    }

    private async Task<bool> TryPublishActivationOutcomeUnknownReportAsync(
        PhraseIndexBuildRun run)
    {
        var reportPublished = await TryWriteReportAsync(
            CreateActivationOutcomeUnknownReport(run),
            run.ReportDirectory);
        if (reportPublished)
        {
            return true;
        }

        AddError(run, "activation-outcome-report-publication-failed");
        return await TryWriteReportAsync(
            CreateActivationOutcomeUnknownReport(run),
            run.ReportDirectory);
    }

    private static PhraseIndexBuildReport CreateActivationOutcomeUnknownReport(
        PhraseIndexBuildRun run) => CreateReport(
            run,
            PhraseIndexBuildOutcome.ActivationOutcomeUnknown,
            "ActivationOutcomeUnknown",
            persistedGeneration: true,
            active: null,
            exactReady: true,
            similarityReady: true);

    private async Task<bool> TryWriteReportAsync(
        PhraseIndexBuildReport report,
        string reportDirectory)
    {
        try
        {
            await reportWriter.WriteAsync(report, reportDirectory, CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void AddError(PhraseIndexBuildRun run, string code)
    {
        if (!run.Errors.Contains(code, StringComparer.Ordinal))
        {
            run.Errors.Add(code);
        }
    }

    private static PhraseIndexBuildReport CreateReport(
        PhraseIndexBuildRun run,
        PhraseIndexBuildOutcome outcome,
        string status,
        bool? persistedGeneration,
        bool? active,
        bool? exactReady,
        bool? similarityReady)
    {
        run.Stopwatch.Stop();
        return new PhraseIndexBuildReport(
            run.BuildId,
            PhraseIndexBuildConstants.FormatVersion.ToString(CultureInfo.InvariantCulture),
            PhraseIndexBuildConstants.BuilderVersion,
            status,
            outcome.ToString(),
            persistedGeneration,
            active,
            exactReady,
            similarityReady,
            run.StartedAtUtc,
            DateTimeOffset.UtcNow,
            run.Stopwatch.ElapsedMilliseconds,
            Math.Max(run.PeakManagedMemoryBytes, GC.GetTotalMemory(false)),
            run.SourceRevision,
            run.SourceFingerprint,
            run.SourceRevisionAtActivation,
            run.SourceFingerprintAtActivation,
            run.ActiveBuildId,
            run.Totals,
            run.DiskPreflight,
            run.Metrics,
            run.Checks,
            run.Warnings,
            run.Errors);
    }

    private static PhraseIndexBuildExecution CreateExecution(
        PhraseIndexBuildRun run,
        PhraseIndexBuildOutcome outcome,
        string message,
        bool reportAvailable,
        bool reportLinked) => new(
            run.BuildId,
            outcome,
            message,
            run.ReportDirectory,
            run.Totals,
            run.SourceFingerprint,
            run.SourceRevision,
            run.ActiveBuildId,
            reportAvailable,
            reportLinked);
}
