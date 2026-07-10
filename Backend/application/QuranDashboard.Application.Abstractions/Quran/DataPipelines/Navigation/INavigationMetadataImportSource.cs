namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public interface INavigationMetadataImportSource
{
    Task<NavigationMetadataSourceData> LoadAsync(
        string sourcePath,
        NavigationExpectedCounts expected,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
