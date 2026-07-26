using QuranDashboard.Application.Abstractions.Abwab.Restore;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public static class AbwabRestoreDependencyInjection
{
    public static IServiceCollection AddAbwabRestoreAdapters(this IServiceCollection services)
    {
        services.AddSingleton<SectionRestoreAdapter>();
        services.AddSingleton<IAbwabRestoreAdapterDescriptor>(sp => sp.GetRequiredService<SectionRestoreAdapter>());

        services.AddSingleton<CategoryRestoreAdapter>();
        services.AddSingleton<IAbwabRestoreAdapterDescriptor>(sp => sp.GetRequiredService<CategoryRestoreAdapter>());

        services.AddSingleton<ManualProtectionRestoreAdapter>();
        services.AddSingleton<IAbwabRestoreAdapterDescriptor>(sp => sp.GetRequiredService<ManualProtectionRestoreAdapter>());

        services.AddSingleton<RelationshipRestoreAdapter>();
        services.AddSingleton<IAbwabRestoreAdapterDescriptor>(sp => sp.GetRequiredService<RelationshipRestoreAdapter>());

        services.AddSingleton<DoorTemplateRestoreAdapter>();
        services.AddSingleton<IAbwabRestoreAdapterDescriptor>(sp => sp.GetRequiredService<DoorTemplateRestoreAdapter>());

        // The interpreter is registered as an event-kind interpreter ONLY — never as an adapter
        // descriptor, so it adds zero entries to the §8 registry. The closed generic below resolves
        // the ONE 029 Category adapter it delegates to and is likewise not a descriptor registration.
        services.AddSingleton<IAbwabRestoreAdapter<CategoryAggregate, CategoryRestoreSnapshot>>(
            sp => sp.GetRequiredService<CategoryRestoreAdapter>());
        services.AddSingleton<TemplateApplicationEventInterpreter>();
        services.AddSingleton<IAbwabRestoreEventInterpreter>(sp => sp.GetRequiredService<TemplateApplicationEventInterpreter>());

        return services;
    }
}
