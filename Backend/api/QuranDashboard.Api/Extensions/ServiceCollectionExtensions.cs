using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using QuranDashboard.Api.Middleware;
using QuranDashboard.Api.Swagger;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "المنهج القرآني API",
                Version = "v1"
            });

            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
            options.UseAllOfForInheritance();
            options.UseOneOfForPolymorphism();
            options.UseAllOfToExtendReferenceSchemas();
            options.SelectDiscriminatorNameUsing(type =>
                type.GetCustomAttribute<JsonPolymorphicAttribute>()?.TypeDiscriminatorPropertyName);
            options.SelectDiscriminatorValueUsing(subType =>
                subType.BaseType?.GetCustomAttributes<JsonDerivedTypeAttribute>()
                    .FirstOrDefault(attribute => attribute.DerivedType == subType)?.TypeDiscriminator?.ToString());
            options.MapType<JsonElement>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = true
            });
            options.SchemaFilter<AllPropertiesRequiredSchemaFilter>();

            var xmlFiles = new[]
            {
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
                "QuranDashboard.Application.Abstractions.xml"
            };
            foreach (var xmlFile in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }
        });
        services.AddHealthChecks()
            .AddDbContextCheck<QuranDashboardDbContext>("database");
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddCors(options =>
        {
            options.AddPolicy("AngularDev", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                var vercelPreviewHostPrefix = configuration["Cors:VercelPreviewHostPrefix"];

                if (allowedOrigins.Length == 0)
                {
                    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
                }

                policy
                    .WithOrigins(allowedOrigins)
                    .SetIsOriginAllowed(origin => IsAllowedCorsOrigin(origin, allowedOrigins, vercelPreviewHostPrefix))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static bool IsAllowedCorsOrigin(
        string origin,
        string[] allowedOrigins,
        string? vercelPreviewHostPrefix)
    {
        if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(vercelPreviewHostPrefix)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
            && uri.Host.StartsWith(vercelPreviewHostPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
