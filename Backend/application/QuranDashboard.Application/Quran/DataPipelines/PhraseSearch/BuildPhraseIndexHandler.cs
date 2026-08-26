using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed class BuildPhraseIndexHandler
{
    private readonly IPhraseIndexBuilder builder;

    public BuildPhraseIndexHandler(IPhraseIndexBuilder builder)
    {
        this.builder = builder;
    }

    public async Task<BuildPhraseIndexResult> HandleAsync(
        BuildPhraseIndexCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ReportRootDirectory);

        var execution = await builder.BuildAsync(
            command.Force,
            Path.GetFullPath(command.ReportRootDirectory),
            ct);

        return new BuildPhraseIndexResult(
            execution.Succeeded,
            MapExitCode(execution.Outcome),
            execution.Message,
            execution.BuildId,
            execution.ReportDirectory,
            execution.Totals,
            execution.SourceFingerprint,
            execution.SourceRevision,
            execution.PreviousBuildId,
            execution.ActiveBuildId);
    }

    private static int MapExitCode(PhraseIndexBuildOutcome outcome) => outcome switch
    {
        PhraseIndexBuildOutcome.Succeeded => BuildPhraseIndexResult.SuccessExitCode,
        PhraseIndexBuildOutcome.Refused => BuildPhraseIndexResult.RefusedExitCode,
        PhraseIndexBuildOutcome.SourceApprovalRequired => BuildPhraseIndexResult.SourceApprovalRequiredExitCode,
        PhraseIndexBuildOutcome.Cancelled => BuildPhraseIndexResult.CancelledExitCode,
        _ => BuildPhraseIndexResult.FailureExitCode,
    };
}
