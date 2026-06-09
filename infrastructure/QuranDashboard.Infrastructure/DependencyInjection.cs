using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Application.Abstractions.Quran.Words.Display;
using QuranDashboard.Infrastructure.Files.Quran.Import;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Import;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display;
using QuranDashboard.Infrastructure.Reports.Quran;
using QuranDashboard.Infrastructure.Reports.Quran.Words;

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

        services.AddSingleton<ManifestReader>();
        services.AddSingleton<JsonWordSourceReader>();
        services.AddSingleton<JsonLayoutSourceReader>();
        services.AddSingleton<JsonMetadataSourceReader>();
        services.AddSingleton<IQuranImportSource, QuranImportSource>();
        services.AddScoped<IQuranImportWriter, EfBulkQuranImportWriter>();
        services.AddSingleton<IImportReportWriter, MarkdownJsonImportReportWriter>();
        services.AddScoped<IDisplayWordsRebuilder, SqlDisplayWordsRebuilder>();
        services.AddSingleton<IDisplayWordsReportWriter, MarkdownJsonDisplayWordsReportWriter>();

        return services;
    }
}
