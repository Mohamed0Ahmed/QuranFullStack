namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceState(
    long SourceRevision,
    string? SourceFingerprint,
    Guid? ActiveBuildId,
    bool IsStale,
    string? StaleReason);
