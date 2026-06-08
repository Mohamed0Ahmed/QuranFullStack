using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Application.Quran.Import.Validation;

namespace QuranDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<QuranFoundationAssembler>();
        services.AddSingleton<QuranImportValidator>();
        services.AddScoped<ImportQuranFoundationHandler>();

        return services;
    }
}
