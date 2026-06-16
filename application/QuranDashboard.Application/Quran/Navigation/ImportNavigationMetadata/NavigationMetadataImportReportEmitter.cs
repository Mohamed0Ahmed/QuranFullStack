using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Application.Quran.Navigation.ImportNavigationMetadata;

internal sealed class NavigationMetadataImportReportEmitter
{
    private readonly INavigationMetadataReportWriter reportWriter;

    public NavigationMetadataImportReportEmitter(INavigationMetadataReportWriter reportWriter)
    {
        this.reportWriter = reportWriter;
    }

    public async Task WriteSuccessAsync(
        NavigationMetadataImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        await WriteOrThrowAsync(report, reportDir, ct);
    }

    public async Task<ImportNavigationMetadataResult?> TryWriteFailureAsync(
        NavigationMetadataImportReport report,
        string reportDir,
        CancellationToken ct) =>
        await TryWriteAsync(report, reportDir, ct);

    public async Task<ImportNavigationMetadataResult?> TryWriteRefusalAsync(
        NavigationMetadataImportReport report,
        string reportDir,
        CancellationToken ct) =>
        await TryWriteAsync(report, reportDir, ct);

    private async Task WriteOrThrowAsync(
        NavigationMetadataImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        try
        {
            await reportWriter.WriteAsync(report, reportDir, ct);
        }
        catch (Exception ex) when (IsReportWriteFailure(ex))
        {
            throw new IOException(
                $"{NavigationMetadataInvariants.ReportRequired} ({ex.Message})",
                ex);
        }
    }

    private async Task<ImportNavigationMetadataResult?> TryWriteAsync(
        NavigationMetadataImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        try
        {
            await reportWriter.WriteAsync(report, reportDir, ct);
            return null;
        }
        catch (Exception ex) when (IsReportWriteFailure(ex))
        {
            return ImportNavigationMetadataResult.Failure(
                $"{NavigationMetadataInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
    }

    private static bool IsReportWriteFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException;
}
