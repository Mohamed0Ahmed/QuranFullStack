using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Application.Abstractions.Quran.Words.Display;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Import;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;
using QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Import;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display;
using QuranDashboard.Infrastructure.Reports.Quran;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;
using QuranDashboard.Infrastructure.Reports.Quran.Irab;
using QuranDashboard.Infrastructure.Reports.Quran.Morphology;
using QuranDashboard.Infrastructure.Reports.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Reports.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Reports.Quran.Words;

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

        services.AddScoped<ITafsirImportSource, UnimplementedTafsirImportSource>();
        services.AddScoped<ITafsirImportWriter, UnimplementedTafsirImportWriter>();
        services.AddSingleton<ITafsirReportWriter, UnimplementedTafsirReportWriter>();

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
