using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

        // IOptions DI is not resolvable before the container is built, so bind the section locally to
        // feed the JwtBearer setup. Values are still validated fail-fast via ValidateOnStart above.
        var authOptions = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()
            ?? new JwtAuthenticationOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.TokenValidationParameters.ValidAudience = authOptions.Audience;

                // Keep raw claim types (notably `sub`, the identity key). Logto issues RFC 9068
                // `at+jwt` access tokens; the default inbound claim-type map would rename `sub`.
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
        });

        return services;
    }
}
