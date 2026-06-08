namespace QuranDashboard.Application.Quran.Import.ImportQuranFoundation;

public sealed record ImportQuranFoundationCommand(
    string SourceRoot,
    string? ReportOutDir,
    bool Force = false);
