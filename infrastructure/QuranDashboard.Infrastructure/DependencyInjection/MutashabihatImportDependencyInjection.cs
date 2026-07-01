using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class MutashabihatImportDependencyInjection
{
    public static IServiceCollection AddMutashabihatImport(this IServiceCollection services)
    {
        services.AddSingleton<MutashabihatManifestReader>();
        services.AddSingleton<JsonPhrasesReader>();
        services.AddSingleton<JsonSimilarAyahReader>();
        services.AddSingleton<MutashabihatAssembler>();
        services.AddScoped<MutashabihatImportSession>();
        services.AddScoped<IMutashabihatImportSource, MutashabihatImportSource>();
        services.AddScoped<IMutashabihatImportWriter, EfBulkMutashabihatWriter>();
        services.AddSingleton<IMutashabihatReportWriter, MarkdownJsonMutashabihatReportWriter>();

        return services;
    }
}
