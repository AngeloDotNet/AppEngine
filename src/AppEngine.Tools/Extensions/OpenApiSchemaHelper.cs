using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace AppEngine.Tools.Extensions;

public static class OpenApiSchemaHelper
{
    public static OpenApiSchema CreateStringSchema(string? defaultValue = null)
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Default = defaultValue is not null ? JsonValue.Create(defaultValue.ToString()) : null
        };

        return schema;
    }
}