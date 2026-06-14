using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Reports.Quran.Tafsirs;

/// <summary>
/// Phase 2 placeholder registration. Replaced by MarkdownJsonTafsirReportWriter in Phase 4 (T056).
/// </summary>
internal sealed class UnimplementedTafsirReportWriter : ITafsirReportWriter
{
    public Task WriteAsync(TafsirImportResult result, string outputDir, CancellationToken ct) =>
        throw new NotImplementedException("Tafsir report writing is implemented in Phase 4.");
}
