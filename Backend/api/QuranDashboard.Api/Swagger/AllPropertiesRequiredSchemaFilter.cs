using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuranDashboard.Api.Swagger;

/// <summary>
/// System.Text.Json serializes every property of the response DTOs (nulls included),
/// so nullable properties are still always present on the wire; mark all properties
/// required so the spec and generated clients reflect that.
/// </summary>
public sealed class AllPropertiesRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        RequireAllProperties(mutableSchema);

        if (mutableSchema.AllOf is not { Count: > 0 })
        {
            return;
        }

        foreach (var part in mutableSchema.AllOf)
        {
            if (part is OpenApiSchema mutablePart)
            {
                RequireAllProperties(mutablePart);
            }
        }
    }

    private static void RequireAllProperties(OpenApiSchema schema)
    {
        if (schema.Properties is not { Count: > 0 })
        {
            return;
        }

        schema.Required ??= new HashSet<string>();
        foreach (var propertyName in schema.Properties.Keys)
        {
            schema.Required.Add(propertyName);
        }
    }
}
