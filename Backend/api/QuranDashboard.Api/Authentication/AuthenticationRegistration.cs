using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Owner;
using QuranDashboard.Api.Authorization.Permissions;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

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
        services.AddScoped<AuthorizationFailureState>();
        services.AddScoped<AuthorizationStateAccessEvaluator>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OwnerAuthorizationHandler>();
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.Owner, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Owner));
            options.AddPolicy(AuthorizationPolicyNames.Admin, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Admin));
            options.AddPolicy(AuthorizationPolicyNames.Editor, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Editor));
        });

        return services;
    }
}
