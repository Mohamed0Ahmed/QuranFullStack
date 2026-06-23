using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class SimpleI3rabGenerationDependencyInjection
{
    public static IServiceCollection AddSimpleI3rabGeneration(this IServiceCollection services)
    {
        services.AddSingleton(I3rabExpectedCounts.Production);
        services.AddSingleton<II3rabRuleCatalog, I3rabRuleCatalogSeed>();
        services.AddSingleton<II3rabAssembler, I3rabAssembler>();
        services.AddScoped<II3rabCommandExecutor, I3rabCommandExecutor>();
        services.AddScoped<II3rabGenerationSource, EfI3rabGenerationSource>();
        services.AddScoped<II3rabGenerationWriteProbe, NullI3rabGenerationWriteProbe>();
        services.AddScoped<II3rabGenerationWriter, EfI3rabGenerationWriter>();
        services.AddScoped<II3rabGenerationReportWriter, MarkdownJsonI3rabReportWriter>();

        return services;
    }
}
