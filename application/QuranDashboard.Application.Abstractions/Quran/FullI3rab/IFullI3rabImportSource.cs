namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public interface IFullI3rabImportSource
{
    Task<FullI3rabSourceData> LoadAsync(
        string sourcePath,
        FullI3rabExpectedCounts expectedCounts,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
