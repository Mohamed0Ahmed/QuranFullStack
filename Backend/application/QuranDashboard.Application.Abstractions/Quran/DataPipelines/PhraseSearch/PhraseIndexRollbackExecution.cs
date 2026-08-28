namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public enum PhraseIndexRollbackOutcome
{
    Succeeded = 1,
    ReconciledAfterCommitFailure = 2,
    Refused = 3,
    RetrySafeFailure = 4,
    RollbackOutcomeUnknown = 5,
}

public enum PhraseIndexRollbackRetryDirective
{
    NotApplicable = 1,
    SafeToRetry = 2,
    DoNotRetry = 3,
}

public sealed record PhraseIndexRollbackExecution(
    PhraseIndexRollbackOutcome Outcome,
    PhraseIndexRollbackRetryDirective RetryDirective,
    string Message,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    long SourceRevision,
    string SourceFingerprint)
{
    public bool Succeeded => Outcome is
        PhraseIndexRollbackOutcome.Succeeded
        or PhraseIndexRollbackOutcome.ReconciledAfterCommitFailure;
}
