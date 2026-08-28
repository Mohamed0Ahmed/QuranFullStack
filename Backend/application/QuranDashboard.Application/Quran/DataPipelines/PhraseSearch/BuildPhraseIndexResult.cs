using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed record BuildPhraseIndexResult(
    bool Succeeded,
    PhraseIndexBuildOutcome Outcome,
    int ExitCode,
    string Message,
    Guid BuildId,
    string ReportDirectory,
    PhraseIndexBuildTotals Totals,
    string SourceFingerprint,
    long SourceRevision,
    Guid? PreviousBuildId,
    Guid? ActiveBuildId,
    bool ReportAvailable,
    bool ReportLinked)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;
    public const int SourceApprovalRequiredExitCode = 3;
    public const int CancelledExitCode = 130;
}
