namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseIndexActivationResult(
    bool Activated,
    string FailureReason,
    long SourceRevisionAtActivation,
    string SourceFingerprintAtActivation,
    Guid? PreviousBuildId,
    Guid? ActiveBuildId);
