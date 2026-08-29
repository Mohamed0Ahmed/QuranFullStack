namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseIndexActivationResult(
    bool OutcomeKnown,
    bool Activated,
    bool ReconciledAfterFailure,
    string FailureReason,
    long SourceRevisionAtActivation,
    string SourceFingerprintAtActivation,
    Guid? ActiveBuildId);
