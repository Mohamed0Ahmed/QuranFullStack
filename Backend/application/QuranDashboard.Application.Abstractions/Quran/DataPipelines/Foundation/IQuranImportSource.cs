namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public interface IQuranImportSource
{
    Task<QuranImportSourceData> LoadAsync(string sourceRoot, CancellationToken ct);
}
