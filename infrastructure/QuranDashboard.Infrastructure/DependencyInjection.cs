using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuranDashboardDb")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:QuranDashboardDb' was not found.");

        services.AddDbContext<QuranDashboardDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
