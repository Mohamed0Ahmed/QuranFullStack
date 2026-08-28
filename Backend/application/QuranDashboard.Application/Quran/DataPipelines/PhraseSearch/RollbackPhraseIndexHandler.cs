using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed class RollbackPhraseIndexHandler
{
    private readonly IPhraseIndexRollback rollback;

    public RollbackPhraseIndexHandler(IPhraseIndexRollback rollback)
    {
        this.rollback = rollback;
    }

    public async Task<RollbackPhraseIndexResult> HandleAsync(
        RollbackPhraseIndexCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var execution = await rollback.RollbackAsync(ct);
        return new RollbackPhraseIndexResult(
            execution.Succeeded,
            execution.Outcome,
            execution.RetryDirective,
            MapExitCode(execution.Outcome),
            execution.Message,
            execution.ActiveBuildId,
            execution.PreviousBuildId,
            execution.SourceRevision,
            execution.SourceFingerprint);
    }

    private static int MapExitCode(PhraseIndexRollbackOutcome outcome) => outcome switch
    {
        PhraseIndexRollbackOutcome.Succeeded
            or PhraseIndexRollbackOutcome.ReconciledAfterCommitFailure =>
            RollbackPhraseIndexResult.SuccessExitCode,
        PhraseIndexRollbackOutcome.RollbackOutcomeUnknown =>
            RollbackPhraseIndexResult.OutcomeUnknownExitCode,
        _ => RollbackPhraseIndexResult.FailureExitCode,
    };
}
