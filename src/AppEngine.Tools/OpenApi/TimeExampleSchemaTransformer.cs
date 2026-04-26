using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi;

public class TimeExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type == typeof(TimeSpan) || type == typeof(TimeSpan?) || type == typeof(TimeOnly) || type == typeof(TimeOnly?))
        {
            schema.Example = JsonValue.Create(DateTime.Now.ToString("HH:mm:ss"));
        }

        return Task.CompletedTask;
    }
}