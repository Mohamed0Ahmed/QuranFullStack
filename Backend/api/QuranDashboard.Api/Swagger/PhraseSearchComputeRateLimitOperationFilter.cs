using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using QuranDashboard.Api.RateLimiting;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuranDashboard.Api.Swagger;

public sealed class PhraseSearchComputeRateLimitOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var usesComputePolicy = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EnableRateLimitingAttribute>()
            .Any(attribute => attribute.PolicyName == PhraseSearchComputePolicy.Name);
        if (!usesComputePolicy)
        {
            return;
        }

        var responseSchema = context.SchemaGenerator.GenerateSchema(
            typeof(ApiResponse<object>),
            context.SchemaRepository);

        operation.Responses![StatusCodes.Status429TooManyRequests.ToString()] = new OpenApiResponse
        {
            Description = "Too Many Requests",
            Headers = new Dictionary<string, IOpenApiHeader>
            {
                [HeaderNames.RetryAfter] = new OpenApiHeader
                {
                    Description = "Seconds until the request may be retried",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32",
                    },
                },
            },
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = responseSchema,
                },
            },
        };
    }
}
