using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using Microsoft.Extensions.Configuration;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class PhraseSearchDependencyInjection
{
    internal static IServiceCollection AddPhraseSearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PhraseIndexOptions>()
            .Bind(configuration.GetSection(PhraseIndexOptions.SectionName))
            .Validate(options => options.RequestTimeoutSeconds > 0)
            .Validate(options => options.CleanupGraceMinutes > 0)
            .Validate(options => options.FailedBuildRetentionDays > 0)
            .Validate(options => options.DiskSafetyBytes > 0)
            .Validate(options => options.VerifiedDatabaseFreeBytes is null or > 0)
            .Validate(options => options.DatabaseStorageProofContract is null
                || string.Equals(
                    options.DatabaseStorageProofContract,
                    PhraseIndexOptions.OperatorStorageProofContract,
                    StringComparison.Ordinal))
            .Validate(options => options.CleanupGraceMinutes * 60 > options.RequestTimeoutSeconds);
        services.AddScoped<PhraseSourceSnapshotReader>();
        services.AddScoped<PhraseSourceStateCoordinator>();
        services.AddScoped<PhraseDatabaseStoragePreflight>();
        services.AddScoped<PhraseIndexBuildDatabase>();
        services.AddScoped<PhraseExactSourcePreparer>();
        services.AddScoped<PhraseExactWindowStager>();
        services.AddScoped<PhraseExactGenerationPersister>();
        services.AddScoped<PhraseIndexExactStager>();
        services.AddScoped<PhraseSimilarityCandidateGenerator>();
        services.AddScoped<PhraseSimilarityEdgeCopier>();
        services.AddScoped<PhraseSimilarityBuilder>();
        services.AddScoped<PhraseIndexValidator>();
        services.AddScoped<PhraseIndexActivator>();
        services.AddScoped<PhraseIndexBuildReportWriter>();
        services.AddScoped<PhraseIndexBuildFinalizer>();
        services.AddScoped<IPhraseIndexBuilder, EfPhraseIndexBuilder>();
        services.AddScoped<IPhraseIndexRollback, PhraseIndexRollbackService>();
        services.AddSingleton<PhraseSearchReadCache>();
        services.AddSingleton<IPhraseSearchReferenceCodec, PhraseSearchReferenceCodec>();
        services.AddScoped<IPhraseRepetitionsReader, EfPhraseRepetitionsReader>();
        services.AddScoped<IPhraseQueryResolutionReader, EfPhraseQueryResolutionReader>();
        services.AddScoped<IPhraseContextReader, EfPhraseContextReader>();
        services.AddScoped<PhraseSimilarityOccurrenceHydrator>();
        services.AddScoped<IPhraseSimilarityReader, EfPhraseSimilarityReader>();
        return services;
    }
}
