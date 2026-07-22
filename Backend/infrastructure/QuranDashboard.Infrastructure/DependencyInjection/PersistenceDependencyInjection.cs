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
            // Provider retries stay OFF: the Abwab commit protocol runs manual transactions, and an
            // execution strategy would re-run non-idempotent work. UseNpgsql without EnableRetryOnFailure
            // keeps a non-retrying strategy.
            options.UseNpgsql(connectionString);

            // Layer-1 write-kernel guard (no-ChangeSet write / physical-delete rejection). Default
            // policy is default-deny for physical deletes.
            options.AddInterceptors(new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));
        });

        return services;
    }
}
