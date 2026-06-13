using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

internal static class MutashabihatTestServiceCollectionExtensions
{
    public static IServiceCollection AddMutashabihatReaderServices(this IServiceCollection services)
    {
        services.AddSingleton<MutashabihatManifestReader>();
        services.AddSingleton<JsonPhrasesReader>();
        services.AddSingleton<JsonSimilarAyahReader>();

        return services;
    }

    public static IServiceCollection AddMutashabihatImportServices(this IServiceCollection services)
    {
        services.AddMutashabihatReaderServices();
        services.AddSingleton<MutashabihatAssembler>();
        services.AddScoped<MutashabihatImportSession>();
        services.AddScoped<MutashabihatImportSource>();
        services.AddScoped<IMutashabihatImportSource>(sp => sp.GetRequiredService<MutashabihatImportSource>());
        services.AddScoped<IMutashabihatImportWriter, EfBulkMutashabihatWriter>();

        return services;
    }
}
