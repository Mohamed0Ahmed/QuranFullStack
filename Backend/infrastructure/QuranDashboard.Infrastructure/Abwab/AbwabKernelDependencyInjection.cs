using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Abwab.Caching;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Infrastructure.Abwab.Time;

namespace QuranDashboard.Infrastructure.Abwab;

// Infrastructure composition of the Abwab write kernel: server clock, the post-commit cache publisher,
// and the barrier-gated audited-commit executor. The stabilization writer registry lives at the API
// composition root (it must see the Application writer types, which Infrastructure does not reference).
public static class AbwabKernelDependencyInjection
{
    public static IServiceCollection AddAbwabKernel(this IServiceCollection services)
    {
        services.AddSingleton<IServerClock, ServerClock>();
        services.AddSingleton<IAbwabCachePublisher, NullAbwabCachePublisher>();
        services.AddScoped<IAbwabWriteExecutor, AbwabAuditedCommitExecutor>();

        return services;
    }
}
