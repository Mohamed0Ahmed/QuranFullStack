using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Persistence.Reads.Access;
using QuranDashboard.Infrastructure.Persistence.Writes.Access;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AccessDependencyInjection
{
    public static IServiceCollection AddAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEmailIdentityNormalizer, EmailIdentityNormalizer>();
        services.AddScoped<IEmailIdentityPreflight, EmailIdentityPreflight>();
        services.AddScoped<AuthorizationSchemaPreflight>();
        services.Configure<LogtoManagementApiOptions>(
            configuration.GetSection(LogtoManagementApiOptions.SectionName));

        services.AddOptions<OwnerBootstrapOptions>()
            .Bind(configuration.GetSection(OwnerBootstrapOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OwnerBootstrapOptions>, OwnerBootstrapOptionsValidator>();

        services.AddMemoryCache();

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IUserRoleResolver, CachedUserRoleResolver>();
        services.AddScoped<IAuthorizationStateResolver, AuthorizationStateResolver>();
        services.AddScoped<IOwnerBootstrapConfigurationSource, OwnerBootstrapConfigurationSource>();
        services.AddScoped<IOwnerReconciliationStore, OwnerReconciliationStore>();
        services.AddScoped<IPermissionCatalogueSynchronizer, PermissionCatalogueSynchronizer>();
        services.AddScoped<IAccessUserReader, EfAccessUserReader>();
        services.AddScoped<IPermissionCatalogueReader, EfPermissionCatalogueReader>();
        services.AddScoped<IAccessAuditReader, EfAccessAuditReader>();
        services.TryAddScoped<IAccessRequestContext, AmbientAccessRequestContext>();
        services.TryAddScoped<IInteractiveIdentityEvidenceValidator, UnavailableInteractiveIdentityEvidenceValidator>();
        services.AddScoped<IAccessAuditAppender, AccessAuditAppender>();
        services.AddScoped<AccessUserMutationTransaction>();
        services.AddScoped<IAccessUserMutationService, EfAccessUserMutationService>();
        services.AddScoped<ILogtoSubjectRelinkService, EfLogtoSubjectRelinkService>();
        services.AddHttpClient<IExternalUserProfileSource, LogtoManagementApiUserProfileSource>();

        return services;
    }
}
