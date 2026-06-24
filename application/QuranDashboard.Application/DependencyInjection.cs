using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Application.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafSurahCatalog;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafStudySourceCatalog;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;

namespace QuranDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<QuranFoundationAssembler>();
        services.AddSingleton<QuranImportValidator>();
        services.AddScoped<ImportQuranFoundationHandler>();
        services.AddScoped<RebuildDisplayWordsHandler>();
        services.AddScoped<ImportMorphologyHandler>();
        services.AddScoped<ImportMutashabihatHandler>();
        services.AddScoped<ImportTafsirsHandler>();
        services.AddScoped<ImportTranslationsHandler>();
        services.AddScoped<ImportNavigationMetadataHandler>();
        services.AddScoped<ImportFullI3rabHandler>();
        services.AddScoped<GenerateI3rabHandler>();
        services.AddScoped<GetMushafPageHandler>();
        services.AddScoped<GetAyahStudyHandler>();
        services.AddScoped<GetMushafSurahCatalogHandler>();
        services.AddScoped<GetMushafStudySourceCatalogHandler>();
        services.AddScoped<GetWordAnalysisHandler>();
        services.AddScoped<GetSimilarAyahsHandler>();
        services.AddScoped<GetAyahMutashabihatHandler>();
        services.AddScoped<GetUniqueWordsPageHandler>();
        services.AddScoped<GetUniqueWordSummaryHandler>();
        services.AddScoped<GetUniqueWordSurahsHandler>();
        services.AddScoped<GetUniqueWordMissingSurahsHandler>();
        services.AddScoped<GetUniqueWordAyahsHandler>();
        services.AddScoped<GetRootsPageHandler>();
        services.AddScoped<GetRootSummaryHandler>();

        return services;
    }
}
