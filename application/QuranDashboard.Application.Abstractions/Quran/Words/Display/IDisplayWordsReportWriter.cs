namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public interface IDisplayWordsReportWriter
{
    Task WriteAsync(DisplayWordsRebuildResult result, string outputDir, CancellationToken ct);
}
