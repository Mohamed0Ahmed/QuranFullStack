using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class NavigationImportDependencyInjection
{
    public static IServiceCollection AddNavigationImport(this IServiceCollection services)
    {
        services.AddSingleton<NavigationManifestReader>();
        services.AddSingleton<JsonNavigationDatasetReader>();
        services.AddSingleton<NavigationMetadataAssembler>();
        services.AddScoped<NavigationMetadataValidationRunner>();
        services.AddScoped<INavigationMetadataImportSource, NavigationMetadataImportSource>();
        services.AddScoped<INavigationMetadataImportWriter, EfBulkNavigationMetadataImportWriter>();
        services.AddSingleton<INavigationMetadataImportReportBuilder, NavigationMetadataImportReportBuilder>();
        services.AddSingleton<INavigationMetadataReportWriter, MarkdownJsonNavigationMetadataReportWriter>();

        return services;
    }
}
