namespace QuranDashboard.Application.Abstractions.Quran.Translations;

public interface ITranslationImportSource
{
    Task<TranslationSourceData> LoadAsync(
        string sourcePath,
        TranslationExpectedCounts expectedCounts,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
