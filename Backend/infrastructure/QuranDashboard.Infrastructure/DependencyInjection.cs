using Microsoft.Extensions.Configuration;
using QuranDashboard.Infrastructure.Abwab;
using QuranDashboard.Infrastructure.Abwab.Notifications;
using QuranDashboard.Infrastructure.Security;
using QuranDashboard.Infrastructure.ServiceRegistration;

namespace QuranDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddAbwabKernel();
        services.AddSecurity();
        services.AddNotificationStorage();
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

        return services;
    }
}
