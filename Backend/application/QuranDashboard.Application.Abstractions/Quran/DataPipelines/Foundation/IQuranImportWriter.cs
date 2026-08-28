namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public interface IQuranImportWriter
{
    Task<QuranImportWriteResult> WriteAsync(AssembledQuranData data, bool force, CancellationToken ct);
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);
}
