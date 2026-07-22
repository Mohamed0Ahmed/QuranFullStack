using Microsoft.Extensions.Configuration;
using QuranDashboard.Infrastructure.Abwab.Persistence;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class PersistenceDependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuranDashboardDb")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:QuranDashboardDb' was not found.");

        services.AddDbContext<QuranDashboardDbContext>(options =>
        {
            // Retries stay OFF: the Abwab commit protocol runs manual transactions; an execution strategy
            // would re-run non-idempotent work.
            options.UseNpgsql(connectionString);

            // Write-kernel guard: default-deny for physical deletes.
            options.AddInterceptors(new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));
        });

        return services;
    }
}
