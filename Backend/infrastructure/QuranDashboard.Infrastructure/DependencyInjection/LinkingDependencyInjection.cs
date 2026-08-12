using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class LinkingDependencyInjection
{
    public static IServiceCollection AddLinking(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LinkingSourceCacheEntryOptions>(
            configuration.GetSection(LinkingSourceCacheEntryOptions.SectionName));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<LinkingSourceCacheEntryOptions>>().Value);

        services.AddSingleton<LinkingSourceResolutionCache>();
        services.AddSingleton<LinkingAyahTextCache>();

        services.AddScoped<EfLinkingSourceResolutionReader>();
        services.AddScoped<ILinkingSourceResolutionReader>(sp => new CachedLinkingSourceResolutionReader(
            sp.GetRequiredService<EfLinkingSourceResolutionReader>(),
            sp.GetRequiredService<LinkingSourceResolutionCache>(),
            sp.GetRequiredService<LinkingAyahTextCache>()));

        services.AddScoped<ILinkingWorkspaceReader, EfLinkingWorkspaceReader>();
        services.AddScoped<ILinkingWorkspaceWriter, EfLinkingWorkspaceWriter>();
        services.AddScoped<ILinkingConfirmedStateReader, EfLinkingConfirmedStateReader>();

        return services;
    }
}
