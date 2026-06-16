namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public interface INavigationMetadataReportWriter
{
    Task WriteAsync(NavigationMetadataImportReport report, string reportOutDir, CancellationToken ct);
}
