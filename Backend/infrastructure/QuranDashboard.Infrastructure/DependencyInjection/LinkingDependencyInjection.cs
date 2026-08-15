using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Infrastructure.Background;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class LinkingDependencyInjection
{
    public static IServiceCollection AddLinking(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LinkingScalabilityOptions>(
            configuration.GetSection(LinkingScalabilityOptions.SectionName));
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LinkingScalabilityOptions>>().Value;
            options.Validate();
            return options;
        });
        services.AddSingleton<ILinkingScalabilityPolicy>(sp =>
            sp.GetRequiredService<LinkingScalabilityOptions>());

        services.AddSingleton<LinkingSourceResolutionCache>();
        services.AddSingleton<LinkingAyahTextCache>();
        services.AddSingleton<ILinkingDataRevisionWriterStore, LinkingDataRevisionStore>();
        services.AddScoped<ILinkingDataRevisionReadScope, EfLinkingDataRevisionReadScope>();
        services.AddScoped<ILinkingDataRevisionReader, EfLinkingDataRevisionReader>();

        services.AddScoped<EfLinkingSourceResolutionReader>();
        services.AddScoped<ILinkingSourcePageReader, CachedLinkingSourcePageReader>();
        services.AddScoped<ILinkingSourcePreparationReader, CachedLinkingSourcePreparationReader>();
        services.AddScoped<ILinkingPreparedPreflightStore, EfLinkingPreparedPreflightStore>();
        services.AddHostedService<LinkingPreparedPreflightProcessorService>();
        services.AddHostedService<LinkingPreparedPreflightCleanupService>();
        services.AddScoped<ILinkingConfirmationJobStore, EfLinkingConfirmationJobStore>();
        services.AddHostedService<LinkingConfirmationJobProcessorService>();
        services.AddHostedService<LinkingConfirmationJobCleanupService>();

        services.AddScoped<ILinkingWorkspaceReader, EfLinkingWorkspaceReader>();
        services.AddScoped<ILinkingWorkspaceWriter, EfLinkingWorkspaceWriter>();
        services.AddScoped<ILinkingConfirmedStateReader, EfLinkingConfirmedStateReader>();
        services.AddScoped<EfLinkingConfirmationWriter>();
        services.AddScoped<ILinkingConfirmationWriter>(sp => new InvalidatingLinkingConfirmationWriter(
            sp.GetRequiredService<EfLinkingConfirmationWriter>(),
            sp.GetRequiredService<IAbwabCacheInvalidator>()));

        return services;
    }
}
