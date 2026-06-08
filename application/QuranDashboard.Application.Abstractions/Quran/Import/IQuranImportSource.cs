namespace QuranDashboard.Application.Abstractions.Quran.Import;

public interface IQuranImportSource
{
    Task<QuranImportSourceData> LoadAsync(string sourceRoot, CancellationToken ct);
}
