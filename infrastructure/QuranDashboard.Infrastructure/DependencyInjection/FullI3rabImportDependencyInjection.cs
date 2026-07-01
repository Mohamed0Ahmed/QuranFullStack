using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.FullI3rab;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class FullI3rabImportDependencyInjection
{
    public static IServiceCollection AddFullI3rabImport(this IServiceCollection services)
    {
        services.AddSingleton<FullI3rabManifestReader>();
        services.AddSingleton<JsonFullI3rabSourceReader>();
        services.AddSingleton<FullI3rabAssembler>();
        services.AddScoped<FullI3rabValidationRunner>();
        services.AddScoped<IFullI3rabImportSource, FullI3rabImportSource>();
        services.AddScoped<IFullI3rabImportWriter, EfBulkFullI3rabImportWriter>();
        services.AddSingleton<IFullI3rabImportReportBuilder, FullI3rabImportReportBuilder>();
        services.AddSingleton<IFullI3rabReportWriter, MarkdownJsonFullI3rabReportWriter>();

        return services;
    }
}
