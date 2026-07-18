using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Registers Logto access-token authentication: JwtBearer validates the token's signature (JWKS and
/// issuer auto-discovered from the <c>Auth:Authority</c> OIDC metadata) and its audience
/// (<c>Auth:Audience</c> = the registered Logto API resource). Raw claims are preserved so the
/// <c>sub</c> identity key survives. A failed challenge emits the shared <see cref="ApiResponse{T}"/>
/// failure envelope instead of the default empty 401. Options are validated fail-fast at startup.
/// Authorization is registered plain — no fallback or named policies yet (Phase 2).
/// </summary>
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
                        // Suppress the framework's default empty 401 and emit the shared failure envelope.
                        context.HandleResponse();
                        var writer = context.HttpContext.RequestServices.GetRequiredService<UnauthorizedRejectionWriter>();
                        await writer.WriteAsync(context.HttpContext, context.HttpContext.RequestAborted);
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }
}
