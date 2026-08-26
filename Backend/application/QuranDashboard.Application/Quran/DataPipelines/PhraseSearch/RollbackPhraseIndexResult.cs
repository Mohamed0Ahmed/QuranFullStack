namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed record RollbackPhraseIndexResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    long SourceRevision,
    string SourceFingerprint)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
}
