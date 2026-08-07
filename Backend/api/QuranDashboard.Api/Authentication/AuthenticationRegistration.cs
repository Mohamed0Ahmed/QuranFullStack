using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Owner;
using QuranDashboard.Api.Authorization.Permissions;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

internal static class AuthenticationRegistration
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtAuthenticationOptions>()
            .Bind(configuration.GetSection(JwtAuthenticationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtAuthenticationOptions>, JwtAuthenticationOptionsValidator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IAccessRequestContext, HttpContextAccessRequestContext>();
        services.AddScoped<IInteractiveIdentityEvidenceValidator, JwtInteractiveIdentityEvidenceValidator>();
        services.AddScoped<AuthorizationFailureState>();
        services.AddScoped<AuthorizationStateAccessEvaluator>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OwnerAuthorizationHandler>();
        services.AddSingleton<UnsafeEndpointMetadataValidator>();
        services.AddSingleton<AuthorizationRejectionWriter>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();

        var authOptions = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()
            ?? new JwtAuthenticationOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.TokenValidationParameters.ValidAudience = authOptions.Audience;

                options.MapInboundClaims = false;
            });

        services.AddAuthorization();

        return services;
    }
}
