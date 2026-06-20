namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

// Infrastructure-internal verdict/severity tokens shared by the morphology import writer and its
// extracted helpers (copier, validation runner, report builder). These are write-path implementation
// details and intentionally separate from the cross-boundary MorphologyInvariants in Abstractions.
internal static class MorphologyImportConstants
{
    public const string PassVerdict = "pass";
    public const string FailVerdict = "fail";
    public const string HardSeverity = "hard";
}
