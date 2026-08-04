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
                    AutoReplenishment = false,
                });
    }
}
