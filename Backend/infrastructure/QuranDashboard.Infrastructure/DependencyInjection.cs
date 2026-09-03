using Microsoft.Extensions.Configuration;
using QuranDashboard.Infrastructure.ServiceRegistration;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services.AddInfrastructure(configuration, DatabaseActivityPolicy.Production);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseActivityPolicy databaseActivityPolicy)
    {
        ArgumentNullException.ThrowIfNull(databaseActivityPolicy);
        services.AddSingleton(databaseActivityPolicy);
        services.AddPersistence(configuration, databaseActivityPolicy);
        services.AddAccess(configuration);
        services.AddMushafReader(configuration);
        services.AddUniqueWords();
        services.AddRoots();
        services.AddLemmas();
        services.AddStems();
        services.AddWordTypes();
        services.AddFoundationImport();
        services.AddMorphologyImport();
        services.AddMutashabihatImport();
        services.AddTafsirsImport();
        services.AddTranslationsImport();
        services.AddNavigationImport();
        services.AddFullI3rabImport();
        services.AddSimpleI3rabGeneration();
        services.AddPhraseSearch(configuration);
        services.AddAbwab();
        services.AddLinking(configuration, databaseActivityPolicy);

        return services;
    }
}
