using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Caching.Abwab;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabDependencyInjection
{
    public static IServiceCollection AddAbwab(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<AbwabCacheGeneration>();
        services.AddSingleton<IAbwabCacheInvalidator>(sp => sp.GetRequiredService<AbwabCacheGeneration>());
        services.AddSingleton<IAbwabCacheValidators>(sp => sp.GetRequiredService<AbwabCacheGeneration>());

        services.AddScoped<EfAbwabSectionsWriter>();
        services.AddScoped<IAbwabSectionsWriter>(sp => new InvalidatingAbwabSectionsWriter(
            sp.GetRequiredService<EfAbwabSectionsWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        services.AddScoped<EfAbwabDoorsWriter>();
        services.AddScoped<IAbwabDoorsWriter>(sp => new InvalidatingAbwabDoorsWriter(
            sp.GetRequiredService<EfAbwabDoorsWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        services.AddScoped<EfAbwabRelationsWriter>();
        services.AddScoped<IAbwabRelationsWriter>(sp => new InvalidatingAbwabRelationsWriter(
            sp.GetRequiredService<EfAbwabRelationsWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        services.AddScoped<EfAbwabTemplatesWriter>();
        services.AddScoped<IAbwabTemplatesWriter>(sp => new InvalidatingAbwabTemplatesWriter(
            sp.GetRequiredService<EfAbwabTemplatesWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        services.AddScoped<EfAbwabTemplateApplyWriter>();
        services.AddScoped<IAbwabTemplateApplyWriter>(sp => new InvalidatingAbwabTemplateApplyWriter(
            sp.GetRequiredService<EfAbwabTemplateApplyWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        services.AddScoped<EfAbwabTreeReader>();
        services.AddScoped<IAbwabTreeReader>(sp => new CachedAbwabTreeReader(
            sp.GetRequiredService<EfAbwabTreeReader>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<AbwabCacheGeneration>()));

        services.AddScoped<EfAbwabTemplatesReader>();
        services.AddScoped<IAbwabTemplatesReader>(sp => new CachedAbwabTemplatesReader(
            sp.GetRequiredService<EfAbwabTemplatesReader>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<AbwabCacheGeneration>()));

        services.AddScoped<IAbwabRelationsReader, EfAbwabRelationsReader>();

        return services;
    }
}
