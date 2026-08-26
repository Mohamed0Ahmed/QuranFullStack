namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceState(
    long SourceRevision,
    string? SourceFingerprint,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    bool IsStale,
    string? StaleReason);
