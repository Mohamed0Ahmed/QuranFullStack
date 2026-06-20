namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

public interface IDisplayWordsReportWriter
{
    Task WriteAsync(DisplayWordsRebuildResult result, string outputDir, CancellationToken ct);
}
