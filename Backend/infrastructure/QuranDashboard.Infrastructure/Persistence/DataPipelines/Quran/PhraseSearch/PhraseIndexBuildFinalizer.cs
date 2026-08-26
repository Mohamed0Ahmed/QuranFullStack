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
        string status,
        bool buildPersisted,
        bool persistedGeneration,
        bool exactReady,
        bool similarityReady)
    {
        var report = CreateReport(
            run,
            outcome,
            status,
            persistedGeneration,
            active: false,
            exactReady,
            similarityReady);
        await reportWriter.WriteAsync(report, run.ReportDirectory, CancellationToken.None);

        if (buildPersisted && connection is not null)
        {
            await database.RecordReportPathAsync(
                connection,
                run.BuildId,
                run.ReportDirectory,
                CancellationToken.None);
            await database.DeleteFailedGenerationRowsAsync(
                connection,
                run.BuildId,
                CancellationToken.None);
            await database.CleanupExpiredFailedBuildAuditsAsync(
                connection,
                CancellationToken.None);
        }

        return CreateExecution(run, outcome, message);
    }

    internal async Task<PhraseIndexBuildExecution> FinishActivatedAsync(
        NpgsqlConnection connection,
        PhraseIndexBuildRun run)
    {
        var cleanupSucceeded = true;
        try
        {
            await database.CleanupEligibleSupersededBuildsAsync(
                connection,
                CancellationToken.None);
        }
        catch (Exception)
        {
            cleanupSucceeded = false;
            run.Warnings.Add(
                "Eligible superseded-build cleanup failed after activation; active and previous builds were retained.");
            run.Errors.Add("post-activation-cleanup-failed");
        }

        run.Checks.Add(new PhraseBuildCheck(
            "POST-ACTIVATION-CLEANUP",
            "operational",
            "completed",
            cleanupSucceeded ? "completed" : "failed",
            cleanupSucceeded));
        var report = CreateReport(
            run,
            PhraseIndexBuildOutcome.Succeeded,
            "Active",
            persistedGeneration: true,
            active: true,
            exactReady: true,
            similarityReady: true);
        await reportWriter.WriteAsync(report, run.ReportDirectory, CancellationToken.None);
        await database.RecordReportPathAsync(
            connection,
            run.BuildId,
            run.ReportDirectory,
            CancellationToken.None);
        var message = cleanupSucceeded
            ? "Phrase index build activated successfully; eligible cleanup completed."
            : "Phrase index build activated successfully, but eligible cleanup failed. See the report.";
        return CreateExecution(run, PhraseIndexBuildOutcome.Succeeded, message);
    }

    private static PhraseIndexBuildReport CreateReport(
        PhraseIndexBuildRun run,
        PhraseIndexBuildOutcome outcome,
        string status,
        bool persistedGeneration,
        bool active,
        bool exactReady,
        bool similarityReady)
    {
        run.Stopwatch.Stop();
        return new PhraseIndexBuildReport(
            run.BuildId,
            PhraseIndexBuildConstants.FormatVersion.ToString(CultureInfo.InvariantCulture),
            PhraseIndexBuildConstants.BuilderVersion,
            status,
            outcome.ToString(),
            run.Force,
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
            run.PreviousBuildId,
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
        string message) => new(
            run.BuildId,
            outcome,
            message,
            run.ReportDirectory,
            run.Totals,
            run.SourceFingerprint,
            run.SourceRevision,
            run.PreviousBuildId,
            run.ActiveBuildId);
}
