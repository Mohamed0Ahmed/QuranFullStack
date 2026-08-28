using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed record RollbackPhraseIndexResult(
    bool Succeeded,
    PhraseIndexRollbackOutcome Outcome,
    PhraseIndexRollbackRetryDirective RetryDirective,
    int ExitCode,
    string Message,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    long SourceRevision,
    string SourceFingerprint)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int OutcomeUnknownExitCode = 2;
}
