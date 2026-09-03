using Microsoft.Extensions.Configuration;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class PersistenceDependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseActivityPolicy databaseActivityPolicy)
    {
        var connectionString = configuration.GetConnectionString("QuranDashboardDb")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:QuranDashboardDb' was not found.");
        connectionString = databaseActivityPolicy.ApplyToConnectionString(connectionString);

        services.AddDbContext<QuranDashboardDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
