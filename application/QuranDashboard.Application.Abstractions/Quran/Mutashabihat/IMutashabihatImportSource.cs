namespace QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

public interface IMutashabihatImportSource
{
    Task<MutashabihatSourceData> LoadAsync(string sourcePath, CancellationToken ct);
    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}

public interface IMutashabihatImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<MutashabihatImportResult> ImportAsync(
        MutashabihatSourceData source,
        bool force,
        MutashabihatExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct);
}

public interface IMutashabihatReportWriter
{
    Task WriteAsync(MutashabihatImportResult result, string outputDir, CancellationToken ct);
}
