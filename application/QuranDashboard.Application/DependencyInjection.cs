using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Application.Quran.Import.Validation;
using QuranDashboard.Application.Quran.Words.ImportMorphology;
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;

namespace QuranDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<QuranFoundationAssembler>();
        services.AddSingleton<QuranImportValidator>();
        services.AddScoped<ImportQuranFoundationHandler>();
        services.AddScoped<RebuildDisplayWordsHandler>();
        services.AddScoped<ImportMorphologyHandler>();

        return services;
    }
}
