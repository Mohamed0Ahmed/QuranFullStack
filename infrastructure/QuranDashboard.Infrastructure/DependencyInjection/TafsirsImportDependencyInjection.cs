using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class TafsirsImportDependencyInjection
{
    public static IServiceCollection AddTafsirsImport(this IServiceCollection services)
    {
        services.AddSingleton<TafsirManifestReader>();
        services.AddSingleton<JsonTafsirSourceReader>();
        services.AddSingleton<TafsirAssembler>();
        services.AddScoped<TafsirValidationRunner>();
        services.AddScoped<ITafsirImportSource, TafsirImportSource>();
        services.AddScoped<ITafsirImportWriter, EfBulkTafsirImportWriter>();
        services.AddSingleton<ITafsirImportReportBuilder, TafsirImportReportBuilder>();
        services.AddSingleton<ITafsirReportWriter, MarkdownJsonTafsirReportWriter>();

        return services;
    }
}
