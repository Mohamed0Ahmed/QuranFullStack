using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Translations;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Translations;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class TranslationsImportDependencyInjection
{
    public static IServiceCollection AddTranslationsImport(this IServiceCollection services)
    {
        services.AddSingleton<TranslationManifestReader>();
        services.AddSingleton<TranslationDisplayMetadataReader>();
        services.AddSingleton<JsonTranslationSourceReader>();
        services.AddSingleton<TranslationAssembler>();
        services.AddScoped<TranslationValidationRunner>();
        services.AddScoped<ITranslationImportSource, TranslationImportSource>();
        services.AddScoped<ITranslationImportWriter, EfBulkTranslationImportWriter>();
        services.AddSingleton<ITranslationImportReportBuilder, TranslationImportReportBuilder>();
        services.AddSingleton<ITranslationReportWriter, MarkdownJsonTranslationReportWriter>();

        return services;
    }
}
