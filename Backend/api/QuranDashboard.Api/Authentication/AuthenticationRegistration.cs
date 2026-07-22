using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using QuranDashboard.Api.Security.Authorization;
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

        services.AddSingleton<UnauthorizedRejectionWriter>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

        var authOptions = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()
            ?? new JwtAuthenticationOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.TokenValidationParameters.ValidAudience = authOptions.Audience;

                // MapInboundClaims=false keeps the raw "sub" (the identity key); the default map would rename it.
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = static async context =>
                    {
                        context.HandleResponse();
                        var writer = context.HttpContext.RequestServices.GetRequiredService<UnauthorizedRejectionWriter>();
                        await writer.WriteAsync(context.HttpContext, context.HttpContext.RequestAborted);
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.Owner, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Owner));
            options.AddPolicy(AuthorizationPolicyNames.Admin, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Admin));
            options.AddPolicy(AuthorizationPolicyNames.Editor, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Editor));

            PermissionAuthorizationRegistration.AddPermissionPolicies(options);
        });

        services.AddPermissionAuthorizationHandlers();

        return services;
    }
}
