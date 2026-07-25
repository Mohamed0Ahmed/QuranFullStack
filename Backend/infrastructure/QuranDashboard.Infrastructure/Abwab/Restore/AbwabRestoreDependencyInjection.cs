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

        return services;
    }
}
