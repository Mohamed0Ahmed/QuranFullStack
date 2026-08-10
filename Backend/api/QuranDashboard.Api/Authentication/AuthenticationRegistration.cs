using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Owner;
using QuranDashboard.Api.Authorization.Permissions;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

internal static class AuthenticationRegistration
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
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
        var e2eTestIssuerTrust = E2ETestIssuerTrust.Create(environment.EnvironmentName, configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                ConfigureBearer(options, authOptions, authOptions.Audience, e2eTestIssuerTrust);
            })
            .AddJwtBearer(InteractiveIdentityEvidenceAuthentication.Scheme, options =>
            {
                ConfigureBearer(options, authOptions, authOptions.InteractiveClientId, e2eTestIssuerTrust);
            });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureBearer(
        JwtBearerOptions options,
        JwtAuthenticationOptions authOptions,
        string audience,
        E2ETestIssuerTrust? e2eTestIssuerTrust)
    {
        options.Authority = authOptions.Authority;
        options.TokenValidationParameters.ValidAudience = audience;
        options.MapInboundClaims = false;

        if (e2eTestIssuerTrust is null)
        {
            return;
        }

        options.TokenValidationParameters.ValidIssuers =
        [
            authOptions.Authority,
            e2eTestIssuerTrust.Issuer,
        ];
        options.TokenValidationParameters.IssuerSigningKeys = e2eTestIssuerTrust.SigningKeys;
        options.Configuration = new OpenIdConnectConfiguration { Issuer = e2eTestIssuerTrust.Issuer };
        foreach (var signingKey in e2eTestIssuerTrust.SigningKeys)
        {
            options.Configuration.SigningKeys.Add(signingKey);
        }
    }
}
