using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Application.Quran.Words.ImportMorphology;

public sealed record ImportMorphologyResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    MorphologyImportTotals? Totals)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportMorphologyResult Success(MorphologyImportTotals totals) =>
        new(true, SuccessExitCode, "Morphology import completed successfully.", totals);

    public static ImportMorphologyResult Refused(string message) =>
        new(false, RefusedExitCode, message, null);

    public static ImportMorphologyResult Failure(string message) =>
        new(false, FailureExitCode, message, null);
}
