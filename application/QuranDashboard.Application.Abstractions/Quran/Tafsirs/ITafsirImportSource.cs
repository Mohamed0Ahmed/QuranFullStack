namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public interface ITafsirImportSource
{
    Task<TafsirSourceData> LoadAsync(string sourcePath, CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
