namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public sealed record PhraseIndexRollbackExecution(
    bool Succeeded,
    string Message,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    long SourceRevision,
    string SourceFingerprint);
