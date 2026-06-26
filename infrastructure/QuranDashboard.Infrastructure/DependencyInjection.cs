using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Infrastructure.ServiceRegistration;

namespace QuranDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMushafReader(configuration);
        services.AddUniqueWords();
        services.AddRoots();
        services.AddLemmas();
        services.AddStems();
        services.AddFoundationImport();
        services.AddMorphologyImport();
        services.AddMutashabihatImport();
        services.AddTafsirsImport();
        services.AddTranslationsImport();
        services.AddNavigationImport();
        services.AddFullI3rabImport();
        services.AddSimpleI3rabGeneration();

        return services;
    }
}
