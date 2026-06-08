namespace QuranDashboard.Application.Abstractions.Quran.Import;

public interface IQuranImportWriter
{
    Task WriteAsync(AssembledQuranData data, bool force, CancellationToken ct);
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);
}
