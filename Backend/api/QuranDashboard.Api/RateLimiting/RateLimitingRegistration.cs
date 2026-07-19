using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QuranDashboard.Api.RateLimiting;

internal static class RateLimitingRegistration
{
    private const string DisabledPartitionKey = "__disabled__";
    private const string OptionsPartitionKey = "__options__";
    private const string SwaggerPartitionKey = "__swagger__";
    private const string SwaggerPathBase = "/swagger";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();

        services.AddSingleton<IClientIpResolver, ClientIpResolver>();
        services.AddSingleton<RateLimitRejectionWriter>();

        services.AddRateLimiter(limiterOptions =>
        {
            // The built-in default is 503; the API contract requires 429.
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(CreatePartition);
            limiterOptions.OnRejected = static async (context, cancellationToken) =>
            {
                var writer = context.HttpContext.RequestServices.GetRequiredService<RateLimitRejectionWriter>();
                await writer.WriteAsync(context, cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(HttpContext context)
    {
        var services = context.RequestServices;
        var options = services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        if (!options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter(DisabledPartitionKey);
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter(OptionsPartitionKey);
        }

        var environment = services.GetRequiredService<IWebHostEnvironment>();
        if (environment.IsDevelopment()
            && context.Request.Path.StartsWithSegments(SwaggerPathBase, StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter(SwaggerPartitionKey);
        }

        var clientIp = services.GetRequiredService<IClientIpResolver>().Resolve(context);

        // Namespaced keys: PartitionedRateLimiter caches the materialized limiter by key (first-wins),
        // so a raw-IP key shared by both profiles would make whichever limiter is created first serve
        // both health and general requests for that IP. The general:/health: prefixes keep them apart.
        return RateLimitRequestClassifier.IsHealthRequest(context.Request.Path)
            ? RateLimitPartition.GetFixedWindowLimiter(
                $"health:{clientIp}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.HealthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.HealthWindowSeconds),
                    QueueLimit = 0,
                })
            : RateLimitPartition.GetTokenBucketLimiter(
                $"general:{clientIp}",
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = options.TokenLimit,
                    TokensPerPeriod = options.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(options.ReplenishmentPeriodSeconds),
                    QueueLimit = options.QueueLimit,
                    // The partition forces AutoReplenishment=false and drives replenishment from its own
                    // timer; setting it explicitly also avoids a redundant per-limiter timer allocation.
                    AutoReplenishment = false,
                });
    }
}
