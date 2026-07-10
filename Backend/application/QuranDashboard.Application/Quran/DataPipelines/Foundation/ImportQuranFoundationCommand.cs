namespace QuranDashboard.Application.Quran.DataPipelines.Foundation;

public sealed record ImportQuranFoundationCommand(
    string SourceRoot,
    string? ReportOutDir,
    bool Force = false);
