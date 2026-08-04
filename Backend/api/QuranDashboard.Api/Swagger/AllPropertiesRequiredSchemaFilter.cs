using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuranDashboard.Api.Swagger;

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
