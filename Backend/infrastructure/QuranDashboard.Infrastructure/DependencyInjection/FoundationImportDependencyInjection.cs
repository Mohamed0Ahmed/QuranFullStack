using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Foundation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Foundation;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class FoundationImportDependencyInjection
{
    public static IServiceCollection AddFoundationImport(this IServiceCollection services)
    {
        services.AddSingleton<ManifestReader>();
        services.AddSingleton<JsonWordSourceReader>();
        services.AddSingleton<JsonMasaqSearchWordsReader>();
        services.AddSingleton<JsonLayoutSourceReader>();
        services.AddSingleton<JsonMetadataSourceReader>();
        services.AddSingleton<IQuranImportSource, QuranImportSource>();
        services.AddScoped<IQuranImportWriter, EfBulkQuranImportWriter>();
        services.AddSingleton<IImportReportWriter, MarkdownJsonImportReportWriter>();
        services.AddScoped<IDisplayWordsRebuilder, SqlDisplayWordsRebuilder>();
        services.AddSingleton<IDisplayWordsReportWriter, MarkdownJsonDisplayWordsReportWriter>();

        return services;
    }
}
