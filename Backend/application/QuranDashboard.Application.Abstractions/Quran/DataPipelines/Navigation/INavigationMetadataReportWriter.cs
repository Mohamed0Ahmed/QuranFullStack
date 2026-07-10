namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public interface INavigationMetadataReportWriter
{
    Task WriteAsync(NavigationMetadataImportReport report, string reportOutDir, CancellationToken ct);
}
