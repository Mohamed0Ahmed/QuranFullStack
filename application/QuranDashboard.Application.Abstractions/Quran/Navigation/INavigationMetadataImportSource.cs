namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public interface INavigationMetadataImportSource
{
    Task<NavigationMetadataSourceData> LoadAsync(
        string sourcePath,
        NavigationExpectedCounts expected,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
