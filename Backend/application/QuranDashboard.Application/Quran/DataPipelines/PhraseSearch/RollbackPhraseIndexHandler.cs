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
            execution.Succeeded
                ? RollbackPhraseIndexResult.SuccessExitCode
                : RollbackPhraseIndexResult.FailureExitCode,
            execution.Message,
            execution.ActiveBuildId,
            execution.PreviousBuildId,
            execution.SourceRevision,
            execution.SourceFingerprint);
    }
}
