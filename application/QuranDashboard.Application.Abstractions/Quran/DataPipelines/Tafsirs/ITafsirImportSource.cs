namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public interface ITafsirImportSource
{
    Task<TafsirSourceData> LoadAsync(
        string sourcePath,
        TafsirExpectedCounts expectedCounts,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
