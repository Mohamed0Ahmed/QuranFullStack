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

        return services;
    }
}
