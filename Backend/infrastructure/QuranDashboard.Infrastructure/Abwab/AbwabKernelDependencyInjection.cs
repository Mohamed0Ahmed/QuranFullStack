using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Abwab.Caching;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Infrastructure.Abwab.Time;

namespace QuranDashboard.Infrastructure.Abwab;

public static class AbwabKernelDependencyInjection
{
    public static IServiceCollection AddAbwabKernel(this IServiceCollection services)
    {
        services.AddSingleton<IServerClock, ServerClock>();
        services.AddSingleton<IAbwabCachePublisher, NullAbwabCachePublisher>();
        services.AddScoped<IAbwabWriteExecutor, AbwabAuditedCommitExecutor>();
        services.AddSingleton<IDeletionReservationChecker, InertDeletionReservationChecker>();

        return services;
    }
}
