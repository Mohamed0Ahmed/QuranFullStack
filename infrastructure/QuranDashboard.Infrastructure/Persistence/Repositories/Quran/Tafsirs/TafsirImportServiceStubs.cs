using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

/// <summary>
/// Phase 2 placeholder registrations. Replaced by real implementations in Phase 3 (US1).
/// </summary>
internal sealed class UnimplementedTafsirImportSource : ITafsirImportSource
{
    public Task<TafsirSourceData> LoadAsync(string sourcePath, CancellationToken ct) =>
        throw new NotImplementedException("Tafsir import source loading is implemented in Phase 3.");

    public Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct) =>
        throw new NotImplementedException("Tafsir source unchanged checks are implemented in Phase 3.");
}

internal sealed class UnimplementedTafsirImportWriter : ITafsirImportWriter
{
    public Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct) =>
        throw new NotImplementedException("Tafsir import writer guards are implemented in Phase 3.");

    public Task<TafsirImportResult> ExecuteAcceptedImportAsync(
        TafsirSourceData source,
        bool force,
        TafsirExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TafsirImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct) =>
        throw new NotImplementedException("Tafsir import writer execution is implemented in Phase 3.");
}
