using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Infrastructure.Caching.Quran.MushafReader;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Translations;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.FullI3rab;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Foundation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Foundation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuranDashboardDb")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:QuranDashboardDb' was not found.");

        services.AddDbContext<QuranDashboardDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        ConfigureMushafReader(services, configuration);

        services.AddSingleton<ManifestReader>();
        services.AddSingleton<JsonWordSourceReader>();
        services.AddSingleton<JsonLayoutSourceReader>();
        services.AddSingleton<JsonMetadataSourceReader>();
        services.AddSingleton<IQuranImportSource, QuranImportSource>();
        services.AddScoped<IQuranImportWriter, EfBulkQuranImportWriter>();
        services.AddSingleton<IImportReportWriter, MarkdownJsonImportReportWriter>();
        services.AddScoped<IDisplayWordsRebuilder, SqlDisplayWordsRebuilder>();
        services.AddSingleton<IDisplayWordsReportWriter, MarkdownJsonDisplayWordsReportWriter>();

        services.AddSingleton<BuckwalterArabicMap>();
        services.AddSingleton<SegmentArabicRenderer>();
        services.AddSingleton<MorphologyManifestReader>();
        services.AddSingleton<JsonAlignedCorpusReader>();
        services.AddSingleton<JsonQulRootReader>();
        services.AddSingleton<JsonQulLemmaReader>();
        services.AddSingleton<JsonQulStemReader>();
        services.AddSingleton<MorphologyAssembler>();
        services.AddScoped<IMorphologyImportSource, MorphologyImportSource>();
        services.AddScoped<IMorphologyImportWriter, EfBulkMorphologyWriter>();
        services.AddSingleton<IMorphologyReportWriter, MarkdownJsonMorphologyReportWriter>();

        services.AddSingleton<MutashabihatManifestReader>();
        services.AddSingleton<JsonPhrasesReader>();
        services.AddSingleton<JsonSimilarAyahReader>();
        services.AddSingleton<MutashabihatAssembler>();
        services.AddScoped<MutashabihatImportSession>();
        services.AddScoped<IMutashabihatImportSource, MutashabihatImportSource>();
        services.AddScoped<IMutashabihatImportWriter, EfBulkMutashabihatWriter>();
        services.AddSingleton<IMutashabihatReportWriter, MarkdownJsonMutashabihatReportWriter>();

        services.AddSingleton<TafsirManifestReader>();
        services.AddSingleton<JsonTafsirSourceReader>();
        services.AddSingleton<TafsirAssembler>();
        services.AddScoped<TafsirValidationRunner>();
        services.AddScoped<ITafsirImportSource, TafsirImportSource>();
        services.AddScoped<ITafsirImportWriter, EfBulkTafsirImportWriter>();
        services.AddSingleton<ITafsirImportReportBuilder, TafsirImportReportBuilder>();
        services.AddSingleton<ITafsirReportWriter, MarkdownJsonTafsirReportWriter>();

        services.AddSingleton<TranslationManifestReader>();
        services.AddSingleton<TranslationDisplayMetadataReader>();
        services.AddSingleton<JsonTranslationSourceReader>();
        services.AddSingleton<TranslationAssembler>();
        services.AddScoped<TranslationValidationRunner>();
        services.AddScoped<ITranslationImportSource, TranslationImportSource>();
        services.AddScoped<ITranslationImportWriter, EfBulkTranslationImportWriter>();
        services.AddSingleton<ITranslationImportReportBuilder, TranslationImportReportBuilder>();
        services.AddSingleton<ITranslationReportWriter, MarkdownJsonTranslationReportWriter>();

        services.AddSingleton<NavigationManifestReader>();
        services.AddSingleton<JsonNavigationDatasetReader>();
        services.AddSingleton<NavigationMetadataAssembler>();
        services.AddScoped<NavigationMetadataValidationRunner>();
        services.AddScoped<INavigationMetadataImportSource, NavigationMetadataImportSource>();
        services.AddScoped<INavigationMetadataImportWriter, EfBulkNavigationMetadataImportWriter>();
        services.AddSingleton<INavigationMetadataImportReportBuilder, NavigationMetadataImportReportBuilder>();
        services.AddSingleton<INavigationMetadataReportWriter, MarkdownJsonNavigationMetadataReportWriter>();

        services.AddSingleton<FullI3rabManifestReader>();
        services.AddSingleton<JsonFullI3rabSourceReader>();
        services.AddSingleton<FullI3rabAssembler>();
        services.AddScoped<FullI3rabValidationRunner>();
        services.AddScoped<IFullI3rabImportSource, FullI3rabImportSource>();
        services.AddScoped<IFullI3rabImportWriter, EfBulkFullI3rabImportWriter>();
        services.AddSingleton<IFullI3rabImportReportBuilder, FullI3rabImportReportBuilder>();
        services.AddSingleton<IFullI3rabReportWriter, MarkdownJsonFullI3rabReportWriter>();

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

    /// <summary>
    /// Wires the Mushaf reader feature. Reader/handler registrations are added by
    /// their own stories (T020–T041); this only binds the configured default
    /// source keys so handlers can resolve <see cref="MushafReaderOptions"/>.
    /// </summary>
    private static void ConfigureMushafReader(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MushafReaderOptions>(configuration.GetSection("MushafReader"));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MushafReaderOptions>>().Value);
        services.AddMemoryCache();

        services.AddScoped<EfMushafPageReader>();
        services.AddScoped<IMushafPageReader>(sp => new CachedMushafPageReader(
            sp.GetRequiredService<EfMushafPageReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfAyahStudyReader>();
        services.AddScoped<IAyahStudyReader>(sp => new CachedAyahStudyReader(
            sp.GetRequiredService<EfAyahStudyReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<IMushafSurahCatalogReader, EfMushafSurahCatalogReader>();
        services.AddScoped<IMushafStudySourceCatalogReader, EfMushafStudySourceCatalogReader>();

        services.AddScoped<EfWordAnalysisReader>();
        services.AddScoped<IWordAnalysisReader>(sp => new CachedWordAnalysisReader(
            sp.GetRequiredService<EfWordAnalysisReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfAyahSimilaritiesReader>();
        services.AddScoped<IAyahSimilaritiesReader>(sp => new CachedAyahSimilaritiesReader(
            sp.GetRequiredService<EfAyahSimilaritiesReader>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<EfAyahMutashabihatReader>();
        services.AddScoped<IAyahMutashabihatReader>(sp => new CachedAyahMutashabihatReader(
            sp.GetRequiredService<EfAyahMutashabihatReader>(),
            sp.GetRequiredService<IMemoryCache>()));
    }
}
