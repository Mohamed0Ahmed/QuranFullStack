using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Caching.Abwab;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabDependencyInjection
{
    public static IServiceCollection AddAbwab(this IServiceCollection services)
    {
        // Idempotent (TryAdd-based), and registered here rather than relied upon from the mushaf module,
        // which a host composing only abwab would never call.
        services.AddMemoryCache();

        // One object behind both interfaces. Registering the two interfaces separately against the same
        // implementation type would build two counters: writers would bump one, controllers would read the
        // other, and every client would be served a permanent 304 with a green build and green tests.
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

        // The relations read is deliberately uncached and unconditional (area README).
        services.AddScoped<IAbwabRelationsReader, EfAbwabRelationsReader>();

        return services;
    }
}
