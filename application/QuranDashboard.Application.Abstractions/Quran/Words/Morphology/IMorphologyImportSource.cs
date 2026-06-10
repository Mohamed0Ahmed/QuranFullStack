namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

public interface IMorphologyImportSource
{
    Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct);
    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}

public interface IMorphologyImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);
    Task<MorphologyImportResult> ImportAsync(
        MorphologySourceData source, bool force, int expectedReadableWords, CancellationToken ct);
}

public interface IMorphologyReportWriter
{
    Task WriteAsync(MorphologyImportResult result, string outputDir, CancellationToken ct);
}
